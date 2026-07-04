using System.Net.Http.Headers;
using System.Text;
using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Carriers.UPS.Configuration;
using TBJ.Integrations.Shipping.Carriers.UPS.Models;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.UPS.Internal;

/// <summary>
/// Wewnętrzna pamięć podręczna tokenów OAuth 2.0 dla UPS API.
/// Cache jest kluczowany po <see cref="UpsAuthInfo.ClientId"/>, dzięki czemu
/// wielu tenantów może współdzielić jeden singleton bez wzajemnego wpływu.
/// Bezpieczna wielowątkowo dzięki <see cref="SemaphoreSlim"/> per-klucz.
/// </summary>
internal sealed class UpsTokenCache
{
    private sealed record CacheEntry(string Token, DateTimeOffset Expiry);

    private readonly Dictionary<string, CacheEntry> _cache = [];
    private readonly Dictionary<string, SemaphoreSlim> _locks = [];
    private readonly object _lockMapLock = new();

    /// <summary>
    /// Pobiera aktualny token dostępu UPS dla danego tenanta.
    /// Jeśli token jest ważny, zwraca go z pamięci podręcznej.
    /// W przeciwnym razie pobiera nowy token z UPS OAuth API.
    /// </summary>
    /// <param name="httpClient">Klient HTTP do wywołań OAuth.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta.</param>
    /// <param name="options">Opcje infrastrukturalne UPS (TokenUrl).</param>
    /// <param name="logger">Logger.</param>
    /// <param name="ct">Token anulowania.</param>
    /// <returns>Aktualny token dostępu.</returns>
    /// <exception cref="ShippingException">Gdy nie udało się pobrać tokenu.</exception>
    public async Task<string> GetTokenAsync(
        HttpClient httpClient,
        UpsAuthInfo authInfo,
        UpsOptions options,
        ILogger logger,
        CancellationToken ct = default)
    {
        var semaphore = GetSemaphore(authInfo.ClientId);

        // Szybka ścieżka bez blokady
        if (TryGetCachedToken(authInfo.ClientId, out var cached))
            return cached!;

        await semaphore.WaitAsync(ct);
        try
        {
            // Podwójne sprawdzenie po wejściu do sekcji krytycznej
            if (TryGetCachedToken(authInfo.ClientId, out cached))
                return cached!;

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{authInfo.ClientId}:{authInfo.ClientSecret}"));

            using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
            });

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new ShippingException(
                    $"UPS: błąd komunikacji podczas pobierania tokenu dostępowego: {ex.Message}",
                    CarrierType.UPS,
                    carrierErrorDetails: ex.Message);
            }

            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new ShippingException(
                    $"UPS: nie udało się pobrać tokenu dostępowego — HTTP {(int)response.StatusCode}: {body}",
                    CarrierType.UPS,
                    statusCode: (int)response.StatusCode,
                    carrierErrorDetails: body);
            }

            var tokenResponse = UpsJsonSerializer.Deserialize<UpsTokenResponse>(body)
                ?? throw new ShippingException(
                    "UPS: pusta odpowiedź przy pobieraniu tokenu dostępowego.",
                    CarrierType.UPS);

            var expiry = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            lock (_lockMapLock)
                _cache[authInfo.ClientId] = new CacheEntry(tokenResponse.AccessToken, expiry);

            logger.LogInformation(
                "UPS: pobrano nowy token dla ClientId {ClientId}, wygasa: {Expiry}",
                authInfo.ClientId,
                expiry);

            return tokenResponse.AccessToken;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private bool TryGetCachedToken(string clientId, out string? token)
    {
        lock (_lockMapLock)
        {
            if (_cache.TryGetValue(clientId, out var entry) &&
                DateTimeOffset.UtcNow.AddSeconds(60) < entry.Expiry)
            {
                token = entry.Token;
                return true;
            }
        }
        token = null;
        return false;
    }

    private SemaphoreSlim GetSemaphore(string clientId)
    {
        lock (_lockMapLock)
        {
            if (!_locks.TryGetValue(clientId, out var sem))
            {
                sem = new SemaphoreSlim(1, 1);
                _locks[clientId] = sem;
            }
            return sem;
        }
    }
}
