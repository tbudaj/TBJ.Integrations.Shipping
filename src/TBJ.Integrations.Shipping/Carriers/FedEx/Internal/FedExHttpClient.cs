using System.Text;
using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Carriers.FedEx.Configuration;
using TBJ.Integrations.Shipping.Carriers.FedEx.Models;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.FedEx.Internal;

/// <summary>
/// Wewnętrzny wrapper nad <see cref="HttpClient"/> wyspecjalizowany do komunikacji
/// z FedEx REST API. Obsługuje uwierzytelnienie OAuth 2.0 (per-tenant), wspólną logikę HTTP,
/// błędy i logowanie.
/// </summary>
internal sealed class FedExHttpClient
{
    private readonly HttpClient _http;
    private readonly FedExOptions _options;
    private readonly FedExTokenCache _tokenCache;
    private readonly ILogger<FedExHttpClient> _logger;

    /// <summary>
    /// Inicjalizuje nową instancję klienta HTTP dla FedEx API.
    /// </summary>
    /// <param name="http">Skonfigurowany klient HTTP (BaseAddress i Timeout ustawione przez DI).</param>
    /// <param name="options">Opcje infrastrukturalne FedEx (TokenUrl).</param>
    /// <param name="tokenCache">Pamięć podręczna tokenów OAuth 2.0 (per-ClientId).</param>
    /// <param name="logger">Logger.</param>
    public FedExHttpClient(HttpClient http, FedExOptions options, FedExTokenCache tokenCache, ILogger<FedExHttpClient> logger)
    {
        _http = http;
        _options = options;
        _tokenCache = tokenCache;
        _logger = logger;
    }

    /// <summary>
    /// Wysyła żądanie GET i zwraca treść odpowiedzi jako string.
    /// </summary>
    /// <param name="path">Ścieżka zasobu.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta — używane do pobrania/cache tokenu OAuth2.</param>
    /// <param name="ct">Token anulowania.</param>
    /// <returns>Treść odpowiedzi HTTP jako string.</returns>
    /// <exception cref="ShippingException">Gdy API zwróci błąd lub komunikacja się nie powiedzie.</exception>
    public async Task<string> GetAsync(string path, FedExAuthInfo authInfo, CancellationToken ct = default)
    {
        _logger.LogDebug("FedEx: GET {Path}", path);

        var token = await _tokenCache.GetTokenAsync(_http, authInfo, _options, _logger, ct);

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ct.IsCancellationRequested)
        {
            throw new ShippingException(
                $"Przekroczono limit czasu oczekiwania na odpowiedź FedEx API dla {path}.",
                CarrierType.FedEx);
        }
        catch (HttpRequestException ex)
        {
            throw new ShippingException(
                $"Błąd komunikacji z FedEx API dla {path}: {ex.Message}",
                CarrierType.FedEx);
        }

        _logger.LogInformation("FedEx: GET {Path} — HTTP {StatusCode}", path, (int)response.StatusCode);

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
    public async Task<string> PostJsonAsync<T>(string path, T body, FedExAuthInfo authInfo, CancellationToken ct = default)
    {
        var json = FedExJsonSerializer.Serialize(body);
        _logger.LogDebug("FedEx: POST {Path} — Body: {Body}", path, json);

        var token = await _tokenCache.GetTokenAsync(_http, authInfo, _options, _logger, ct);

        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ct.IsCancellationRequested)
        {
            throw new ShippingException(
                $"Przekroczono limit czasu oczekiwania na odpowiedź FedEx API dla {path}.",
                CarrierType.FedEx);
        }
        catch (HttpRequestException ex)
        {
            throw new ShippingException(
                $"Błąd komunikacji z FedEx API dla {path}: {ex.Message}",
                CarrierType.FedEx);
        }

        _logger.LogInformation("FedEx: POST {Path} — HTTP {StatusCode}", path, (int)response.StatusCode);

        return await ReadSuccessBodyAsync(response, path, ct);
    }

    /// <summary>
    /// Odczytuje treść odpowiedzi. Gdy błąd, parsuje strukturę błędu FedEx i rzuca wyjątek.
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
                    var errorResp = FedExJsonSerializer.Deserialize<FedExErrorResponse>(body);
                    var firstError = errorResp?.Errors?.FirstOrDefault();
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
                $"FedEx API zwróciło błąd HTTP {(int)resp.StatusCode} dla {path}: {errorMessage ?? body}",
                CarrierType.FedEx,
                statusCode: (int)resp.StatusCode,
                carrierErrorCode: errorCode,
                carrierErrorDetails: errorMessage ?? body);
        }

        return body;
    }
}
