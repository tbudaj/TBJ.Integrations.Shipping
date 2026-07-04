using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Carriers.FedEx.Configuration;
using TBJ.Integrations.Shipping.Carriers.FedEx.Models;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.FedEx.Internal;

/// <summary>
/// Wewnętrzna pamięć podręczna tokenów OAuth 2.0 dla FedEx API.
/// Cache jest kluczowany po <see cref="FedExAuthInfo.ClientId"/>, dzięki czemu
/// wielu tenantów może współdzielić jeden singleton bez wzajemnego wpływu.
/// Bezpieczna wielowątkowo dzięki <see cref="SemaphoreSlim"/> per-klucz.
/// </summary>
internal sealed class FedExTokenCache
{
    private sealed record CacheEntry(string Token, DateTimeOffset Expiry);

    private readonly Dictionary<string, CacheEntry> _cache = [];
    private readonly Dictionary<string, SemaphoreSlim> _locks = [];
    private readonly object _lockMapLock = new();

    /// <summary>
    /// Pobiera aktualny token dostępu FedEx dla danego tenanta.
    /// Jeśli token jest ważny, zwraca go z pamięci podręcznej.
    /// W przeciwnym razie pobiera nowy token z FedEx OAuth API.
    /// </summary>
    /// <param name="httpClient">Klient HTTP do wywołań OAuth.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta.</param>
    /// <param name="options">Opcje infrastrukturalne FedEx (TokenUrl).</param>
    /// <param name="logger">Logger.</param>
    /// <param name="ct">Token anulowania.</param>
    /// <returns>Aktualny token dostępu.</returns>
    /// <exception cref="ShippingException">Gdy nie udało się pobrać tokenu.</exception>
    public async Task<string> GetTokenAsync(
        HttpClient httpClient,
        FedExAuthInfo authInfo,
        FedExOptions options,
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

            using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenUrl);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = authInfo.ClientId,
                ["client_secret"] = authInfo.ClientSecret,
            });

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new ShippingException(
                    $"FedEx: błąd komunikacji podczas pobierania tokenu dostępowego: {ex.Message}",
                    CarrierType.FedEx,
                    carrierErrorDetails: ex.Message);
            }

            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new ShippingException(
                    $"FedEx: nie udało się pobrać tokenu dostępowego — HTTP {(int)response.StatusCode}: {body}",
                    CarrierType.FedEx,
                    statusCode: (int)response.StatusCode,
                    carrierErrorDetails: body);
            }

            var tokenResponse = FedExJsonSerializer.Deserialize<FedExTokenResponse>(body)
                ?? throw new ShippingException(
                    "FedEx: pusta odpowiedź przy pobieraniu tokenu dostępowego.",
                    CarrierType.FedEx);

            var expiry = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            lock (_lockMapLock)
                _cache[authInfo.ClientId] = new CacheEntry(tokenResponse.AccessToken, expiry);

            logger.LogInformation(
                "FedEx: pobrano nowy token dla ClientId {ClientId}, wygasa: {Expiry}",
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
