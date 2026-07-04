using System.Net.Http.Headers;
using System.Text;
using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Carriers.InPost.Models;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.InPost.Internal;

/// <summary>
/// Wewnętrzny wrapper nad <see cref="HttpClient"/> wyspecjalizowany do komunikacji
/// z InPost ShipX API. Obsługuje wspólną logikę HTTP, błędy i logowanie.
/// Token Bearer jest ustawiany per-żądanie na podstawie <see cref="InPostAuthInfo"/> tenanta.
/// </summary>
internal sealed class InPostHttpClient
{
    private readonly HttpClient _http;
    private readonly ILogger<InPostHttpClient> _logger;

    /// <summary>
    /// Inicjalizuje nową instancję klienta HTTP dla InPost ShipX API.
    /// </summary>
    /// <param name="http">Skonfigurowany klient HTTP (BaseAddress i Timeout ustawione przez DI).</param>
    /// <param name="logger">Logger.</param>
    public InPostHttpClient(HttpClient http, ILogger<InPostHttpClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Wysyła żądanie GET i zwraca treść odpowiedzi jako string.
    /// </summary>
    /// <param name="endpoint">Ścieżka zasobu (np. <c>/v1/tracking/123456789</c>).</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta — token Bearer.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Treść odpowiedzi HTTP jako string.</returns>
    /// <exception cref="ShippingException">Gdy API zwróci błąd lub komunikacja się nie powiedzie.</exception>
    public async Task<string> GetAsync(string endpoint, InPostAuthInfo authInfo, CancellationToken cancellationToken = default,
        CarrierType carrier = CarrierType.InPostLocker)
    {
        _logger.LogDebug("InPost: GET {Endpoint}", endpoint);

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authInfo.AccessToken);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !cancellationToken.IsCancellationRequested)
        {
            throw new ShippingException(
                $"Przekroczono limit czasu oczekiwania na odpowiedź InPost API dla {endpoint}.",
                carrier);
        }
        catch (HttpRequestException ex)
        {
            throw new ShippingException(
                $"Błąd komunikacji z InPost API dla {endpoint}: {ex.Message}",
                carrier);
        }

        _logger.LogInformation("InPost: GET {Endpoint} — HTTP {StatusCode}", endpoint, (int)response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            ThrowApiError(endpoint, (int)response.StatusCode, body, carrier);

        return body;
    }

    /// <summary>
    /// Wysyła żądanie GET i zwraca zawartość jako bajty oraz typ MIME.
    /// Używane do pobierania etykiet.
    /// </summary>
    /// <param name="endpoint">Ścieżka zasobu.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta — token Bearer.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Krotka z danymi binarnymi i typem MIME.</returns>
    /// <exception cref="ShippingException">Gdy API zwróci błąd lub komunikacja się nie powiedzie.</exception>
    public async Task<(byte[] Content, string ContentType)> GetBytesAsync(
        string endpoint,
        InPostAuthInfo authInfo,
        CancellationToken cancellationToken = default,
        CarrierType carrier = CarrierType.InPostLocker)
    {
        _logger.LogDebug("InPost: GET bytes {Endpoint}", endpoint);

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authInfo.AccessToken);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !cancellationToken.IsCancellationRequested)
        {
            throw new ShippingException(
                $"Przekroczono limit czasu oczekiwania na odpowiedź InPost API dla {endpoint}.",
                carrier);
        }
        catch (HttpRequestException ex)
        {
            throw new ShippingException(
                $"Błąd komunikacji z InPost API dla {endpoint}: {ex.Message}",
                carrier);
        }

        _logger.LogInformation("InPost: GET bytes {Endpoint} — HTTP {StatusCode}", endpoint, (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            ThrowApiError(endpoint, (int)response.StatusCode, errorBody, carrier);
        }

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return (content, contentType);
    }

    /// <summary>
    /// Wysyła żądanie POST z ciałem JSON i zwraca treść odpowiedzi jako string.
    /// </summary>
    /// <typeparam name="T">Typ serializowanego ciała żądania.</typeparam>
    /// <param name="endpoint">Ścieżka zasobu.</param>
    /// <param name="body">Obiekt do serializacji jako JSON.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta — token Bearer.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Treść odpowiedzi HTTP jako string.</returns>
    /// <exception cref="ShippingException">Gdy API zwróci błąd lub komunikacja się nie powiedzie.</exception>
    public async Task<string> PostJsonAsync<T>(string endpoint, T body, InPostAuthInfo authInfo, CancellationToken cancellationToken = default,
        CarrierType carrier = CarrierType.InPostLocker)
    {
        var json = InPostJsonSerializer.Serialize(body);
        _logger.LogDebug("InPost: POST {Endpoint} — Body: {Body}", endpoint, json);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authInfo.AccessToken);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !cancellationToken.IsCancellationRequested)
        {
            throw new ShippingException(
                $"Przekroczono limit czasu oczekiwania na odpowiedź InPost API dla {endpoint}.",
                carrier);
        }
        catch (HttpRequestException ex)
        {
            throw new ShippingException(
                $"Błąd komunikacji z InPost API dla {endpoint}: {ex.Message}",
                carrier);
        }

        _logger.LogInformation("InPost: POST {Endpoint} — HTTP {StatusCode}", endpoint, (int)response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("InPost: response body for {Endpoint}: {Body}", endpoint, responseBody);

        if (!response.IsSuccessStatusCode)
            ThrowApiError(endpoint, (int)response.StatusCode, responseBody, carrier);

        return responseBody;
    }

    /// <summary>
    /// Analizuje odpowiedź błędu i rzuca <see cref="ShippingException"/> z danymi z InPost API.
    /// </summary>
    private static void ThrowApiError(string endpoint, int statusCode, string body, CarrierType carrier)
    {
        string? errorCode = null;
        string? errorDetails = null;

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var error = InPostJsonSerializer.Deserialize<InPostErrorResponse>(body);
                if (error is not null)
                {
                    errorCode = error.Error;
                    errorDetails = error.Message;

                    if (error.Details is { Count: > 0 })
                    {
                        var detailMessages = error.Details
                            .Select(d => $"{d.Field}: {d.Message}")
                            .ToList();
                        errorDetails = $"{errorDetails} [{string.Join("; ", detailMessages)}]";
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Ignorujemy błąd parsowania — zgłosimy ogólny wyjątek.
                errorDetails = body;
            }
        }

        throw new ShippingException(
            $"InPost API zwróciło błąd HTTP {statusCode} dla {endpoint}: {errorDetails ?? body}",
            carrier,
            statusCode,
            errorCode,
            errorDetails);
    }
}
