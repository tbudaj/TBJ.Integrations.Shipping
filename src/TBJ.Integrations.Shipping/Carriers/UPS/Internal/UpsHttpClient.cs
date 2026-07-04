using System.Text;
using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Carriers.UPS.Configuration;
using TBJ.Integrations.Shipping.Carriers.UPS.Models;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.UPS.Internal;

/// <summary>
/// Wewnętrzny wrapper nad <see cref="HttpClient"/> wyspecjalizowany do komunikacji
/// z UPS REST API. Obsługuje uwierzytelnienie OAuth 2.0 (per-tenant), wspólną logikę HTTP,
/// błędy i logowanie.
/// </summary>
internal sealed class UpsHttpClient
{
    private readonly HttpClient _http;
    private readonly UpsOptions _options;
    private readonly UpsTokenCache _tokenCache;
    private readonly ILogger<UpsHttpClient> _logger;

    /// <summary>
    /// Inicjalizuje nową instancję klienta HTTP dla UPS API.
    /// </summary>
    /// <param name="http">Skonfigurowany klient HTTP (BaseAddress i Timeout ustawione przez DI).</param>
    /// <param name="options">Opcje infrastrukturalne UPS (TokenUrl, DefaultServiceCode).</param>
    /// <param name="tokenCache">Pamięć podręczna tokenów OAuth 2.0 (per-ClientId).</param>
    /// <param name="logger">Logger.</param>
    public UpsHttpClient(HttpClient http, UpsOptions options, UpsTokenCache tokenCache, ILogger<UpsHttpClient> logger)
    {
        _http = http;
        _options = options;
        _tokenCache = tokenCache;
        _logger = logger;
    }

    /// <summary>
    /// Wysyła żądanie GET i zwraca treść odpowiedzi jako string.
    /// </summary>
    /// <param name="path">Ścieżka zasobu (np. <c>/track/v1/details/1Z12345E0205271688</c>).</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta — używane do pobrania/cache tokenu OAuth2.</param>
    /// <param name="ct">Token anulowania.</param>
    /// <returns>Treść odpowiedzi HTTP jako string.</returns>
    /// <exception cref="ShippingException">Gdy API zwróci błąd lub komunikacja się nie powiedzie.</exception>
    public async Task<string> GetAsync(string path, UpsAuthInfo authInfo, CancellationToken ct = default)
    {
        _logger.LogDebug("UPS: GET {Path}", path);

        var token = await _tokenCache.GetTokenAsync(_http, authInfo, _options, _logger, ct);

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("transactionSrc", "TBJ");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ct.IsCancellationRequested)
        {
            throw new ShippingException(
                $"Przekroczono limit czasu oczekiwania na odpowiedź UPS API dla {path}.",
                CarrierType.UPS);
        }
        catch (HttpRequestException ex)
        {
            throw new ShippingException(
                $"Błąd komunikacji z UPS API dla {path}: {ex.Message}",
                CarrierType.UPS);
        }

        _logger.LogInformation("UPS: GET {Path} — HTTP {StatusCode}", path, (int)response.StatusCode);

        return await ReadSuccessBodyAsync(response, path, ct);
    }

    /// <summary>
    /// Wysyła żądanie POST z ciałem JSON i zwraca treść odpowiedzi jako string.
    /// </summary>
    /// <typeparam name="T">Typ serializowanego ciała żądania.</typeparam>
    /// <param name="path">Ścieżka zasobu.</param>
    /// <param name="body">Obiekt do serializacji jako JSON.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta — używane do pobrania/cache tokenu OAuth2.</param>
    /// <param name="ct">Token anulowania.</param>
    /// <returns>Treść odpowiedzi HTTP jako string.</returns>
    /// <exception cref="ShippingException">Gdy API zwróci błąd lub komunikacja się nie powiedzie.</exception>
    public async Task<string> PostJsonAsync<T>(string path, T body, UpsAuthInfo authInfo, CancellationToken ct = default)
    {
        var json = UpsJsonSerializer.Serialize(body);
        _logger.LogDebug("UPS: POST {Path} — Body: {Body}", path, json);

        var token = await _tokenCache.GetTokenAsync(_http, authInfo, _options, _logger, ct);

        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("transactionSrc", "TBJ");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ct.IsCancellationRequested)
        {
            throw new ShippingException(
                $"Przekroczono limit czasu oczekiwania na odpowiedź UPS API dla {path}.",
                CarrierType.UPS);
        }
        catch (HttpRequestException ex)
        {
            throw new ShippingException(
                $"Błąd komunikacji z UPS API dla {path}: {ex.Message}",
                CarrierType.UPS);
        }

        _logger.LogInformation("UPS: POST {Path} — HTTP {StatusCode}", path, (int)response.StatusCode);

        return await ReadSuccessBodyAsync(response, path, ct);
    }

    /// <summary>
    /// Odczytuje treść odpowiedzi. Gdy błąd, parsuje strukturę błędu UPS i rzuca wyjątek.
    /// </summary>
    private async Task<string> ReadSuccessBodyAsync(HttpResponseMessage resp, string path, CancellationToken ct)
    {
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            string? errorCode = null;
            string? errorMessage = null;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    var errorResp = UpsJsonSerializer.Deserialize<UpsErrorResponse>(body);
                    var firstError = errorResp?.Response?.Errors?.FirstOrDefault();
                    if (firstError != null)
                    {
                        errorCode = firstError.Code;
                        errorMessage = firstError.Message;
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    errorMessage = body;
                }
            }

            throw new ShippingException(
                $"UPS API zwróciło błąd HTTP {(int)resp.StatusCode} dla {path}: {errorMessage ?? body}",
                CarrierType.UPS,
                statusCode: (int)resp.StatusCode,
                carrierErrorCode: errorCode,
                carrierErrorDetails: errorMessage ?? body);
        }

        return body;
    }
}
