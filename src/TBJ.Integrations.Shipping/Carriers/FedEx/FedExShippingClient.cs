using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.FedEx.Configuration;
using TBJ.Integrations.Shipping.Carriers.FedEx.Internal;
using TBJ.Integrations.Shipping.Carriers.FedEx.Mappers;
using TBJ.Integrations.Shipping.Carriers.FedEx.Models;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.FedEx;

/// <summary>
/// Implementacja adaptera kurierskiego dla FedEx.
/// Komunikuje się z FedEx REST API przez <see cref="FedExHttpClient"/>.
/// </summary>
internal sealed class FedExShippingClient : IShippingClient
{
    private readonly FedExHttpClient _http;
    private readonly FedExOptions _options;
    private readonly ILogger<FedExShippingClient> _logger;

    /// <summary>
    /// Inicjalizuje adapter FedEx.
    /// </summary>
    /// <param name="http">Wewnętrzny klient HTTP FedEx.</param>
    /// <param name="options">Opcje konfiguracyjne FedEx.</param>
    /// <param name="logger">Logger.</param>
    public FedExShippingClient(FedExHttpClient http, FedExOptions options, ILogger<FedExShippingClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public CarrierType Carrier => CarrierType.FedEx;

    /// <inheritdoc />
    public async Task<ShipmentResult> CreateShipmentAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authInfo = ResolveAuth(request.AuthInfo);

        _logger.LogInformation("FedEx: rejestracja nowej przesyłki");

        var fedExRequest = FedExMapper.ToFedExShipRequest(request, authInfo);
        _logger.LogDebug("FedEx: wysyłanie żądania rejestracji przesyłki");

        var responseJson = await _http.PostJsonAsync("/ship/v1/shipments", fedExRequest, authInfo, cancellationToken);

        var fedExResponse = FedExJsonSerializer.Deserialize<FedExShipResponse>(responseJson)
            ?? throw new ShippingException(
                "FedEx API zwróciło pustą odpowiedź przy rejestracji przesyłki.",
                CarrierType.FedEx);

        var result = FedExMapper.ToShipmentResult(fedExResponse);

        _logger.LogInformation(
            "FedEx: przesyłka zarejestrowana — TrackingNumber: {TrackingNumber}",
            result.TrackingNumber);

        return new ShipmentResult
        {
            TrackingNumber = result.TrackingNumber,
            CarrierShipmentId = result.CarrierShipmentId,
            Label = result.Label,
            RawCarrierResponse = responseJson,
        };
    }

    /// <inheritdoc />
    public async Task<PickupResult> OrderPickupAsync(
        OrderPickupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ShipmentIds.Count == 0)
            throw new ArgumentException("Lista przesyłek do odbioru nie może być pusta.", nameof(request));

        var authInfo = ResolveAuth(request.AuthInfo);

        _logger.LogInformation(
            "FedEx: zamawianie odbioru dla {Count} przesyłek w dniu {PickupDate}",
            request.ShipmentIds.Count,
            request.PickupDate);

        var fedExRequest = FedExMapper.ToFedExPickupRequest(request, authInfo);
        var responseJson = await _http.PostJsonAsync("/pickup/v1/pickups", fedExRequest, authInfo, cancellationToken);

        var fedExResponse = FedExJsonSerializer.Deserialize<FedExPickupResponse>(responseJson)
            ?? throw new ShippingException(
                "FedEx API zwróciło pustą odpowiedź przy zamawianiu odbioru.",
                CarrierType.FedEx);

        var result = FedExMapper.ToPickupResult(fedExResponse);

        _logger.LogInformation(
            "FedEx: odbiór zamówiony — ConfirmationCode: {PickupOrderId}",
            result.PickupOrderId);

        return new PickupResult
        {
            PickupOrderId = result.PickupOrderId,
            ConfirmedPickupWindow = result.ConfirmedPickupWindow,
            RawCarrierResponse = responseJson,
        };
    }

    /// <inheritdoc />
    public async Task<TrackingResult> TrackShipmentAsync(
        CarrierAuthInfo? authInfo,
        string trackingNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        var fedExAuthInfo = ResolveAuth(authInfo);

        _logger.LogInformation("FedEx: śledzenie przesyłki {TrackingNumber}", trackingNumber);

        var trackRequest = new FedExTrackRequest
        {
            TrackingInfo = new List<FedExTrackingInfo>
            {
                new FedExTrackingInfo
                {
                    TrackingNumberInfo = new FedExTrackingNumberInfo
                    {
                        TrackingNumber = trackingNumber,
                    },
                },
            },
            IncludeDetailedScans = true,
        };

        var responseJson = await _http.PostJsonAsync("/track/v1/trackingnumbers", trackRequest, fedExAuthInfo, cancellationToken);

        var fedExResponse = FedExJsonSerializer.Deserialize<FedExTrackResponse>(responseJson)
            ?? throw new ShippingException(
                $"FedEx API zwróciło pustą odpowiedź śledzenia dla {trackingNumber}.",
                CarrierType.FedEx);

        var result = FedExMapper.ToTrackingResult(fedExResponse, trackingNumber);

        _logger.LogInformation(
            "FedEx: status przesyłki {TrackingNumber} — {Status}: {Description}",
            trackingNumber,
            result.CurrentStatus,
            result.StatusDescription);

        return new TrackingResult
        {
            TrackingNumber = result.TrackingNumber,
            CurrentStatus = result.CurrentStatus,
            StatusDescription = result.StatusDescription,
            EstimatedDelivery = result.EstimatedDelivery,
            Events = result.Events,
            RawCarrierResponse = responseJson,
        };
    }

    /// <inheritdoc />
    public async Task<DeliveryConfirmationResult> GetDeliveryConfirmationAsync(
        CarrierAuthInfo? authInfo,
        string trackingNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        var fedExAuthInfo = ResolveAuth(authInfo);

        _logger.LogInformation("FedEx: pobieranie potwierdzenia doręczenia {TrackingNumber}", trackingNumber);

        var trackRequest = new FedExTrackRequest
        {
            TrackingInfo = new List<FedExTrackingInfo>
            {
                new FedExTrackingInfo
                {
                    TrackingNumberInfo = new FedExTrackingNumberInfo
                    {
                        TrackingNumber = trackingNumber,
                    },
                },
            },
            IncludeDetailedScans = true,
        };

        var responseJson = await _http.PostJsonAsync("/track/v1/trackingnumbers", trackRequest, fedExAuthInfo, cancellationToken);

        var fedExResponse = FedExJsonSerializer.Deserialize<FedExTrackResponse>(responseJson)
            ?? throw new ShippingException(
                $"FedEx API zwróciło pustą odpowiedź śledzenia dla {trackingNumber}.",
                CarrierType.FedEx);

        var trackResult = fedExResponse.Output?.CompleteTrackResults?.FirstOrDefault()?.TrackResults?.FirstOrDefault();
        var latestStatus = trackResult?.LatestStatusDetail;

        if (latestStatus == null ||
            (latestStatus.DerivedCode?.ToUpperInvariant() != "DL" &&
             latestStatus.Code?.ToUpperInvariant() != "DL"))
        {
            // Sprawdzamy też w zdarzeniach
            var hasDelivered = trackResult?.ScanEvents?.Any(e =>
                e.DerivedStatusCode?.ToUpperInvariant() == "DL" ||
                e.EventType?.ToUpperInvariant() == "DL") ?? false;

            if (!hasDelivered)
            {
                throw new ShippingException(
                    $"Przesyłka {trackingNumber} nie została jeszcze dostarczona.",
                    CarrierType.FedEx);
            }
        }

        var result = FedExMapper.ToDeliveryConfirmationResult(fedExResponse, trackingNumber);

        _logger.LogInformation(
            "FedEx: potwierdzenie doręczenia {TrackingNumber} — {DeliveredAt}, odebrane przez: {ReceivedBy}",
            trackingNumber,
            result.DeliveredAt,
            result.ReceivedBy);

        return new DeliveryConfirmationResult
        {
            TrackingNumber = result.TrackingNumber,
            DeliveredAt = result.DeliveredAt,
            ReceivedBy = result.ReceivedBy,
            PodDocument = result.PodDocument,
            PodContentType = result.PodContentType,
            RawCarrierResponse = responseJson,
        };
    }

    /// <summary>
    /// Rozwiązuje credentials FedEx na podstawie priorytetu:
    /// per-request <see cref="FedExAuthInfo"/> (Scenariusz A) → Default* z opcji (Scenariusz B).
    /// </summary>
    /// <param name="authInfo">Dane uwierzytelniające z żądania (opcjonalne).</param>
    /// <returns>Zrealizowane <see cref="FedExAuthInfo"/>.</returns>
    /// <exception cref="InvalidOperationException">Gdy brak credentials w obu źródłach.</exception>
    private FedExAuthInfo ResolveAuth(CarrierAuthInfo? authInfo)
    {
        if (authInfo is FedExAuthInfo fedExAuth)
        {
            // Scenariusz A: credentials tenanta przekazane per-request
            _logger.LogDebug("FedEx: używam credentials tenanta (FedExAuthInfo per-request)");
            return fedExAuth;
        }

        if (authInfo is not null)
            throw new InvalidOperationException(
                $"Nieprawidłowy typ AuthInfo dla FedEx: oczekiwano {nameof(FedExAuthInfo)}, otrzymano {authInfo.GetType().Name}.");

        // Scenariusz B: fallback na credentials naszego konta z appsettings
        if (!string.IsNullOrWhiteSpace(_options.DefaultClientId) &&
            !string.IsNullOrWhiteSpace(_options.DefaultClientSecret) &&
            !string.IsNullOrWhiteSpace(_options.DefaultAccountNumber))
        {
            _logger.LogInformation("FedEx: brak AuthInfo per-request — używam domyślnych credentials z FedExOptions (Scenariusz B)");
            return new FedExAuthInfo
            {
                ClientId      = _options.DefaultClientId,
                ClientSecret  = _options.DefaultClientSecret,
                AccountNumber = _options.DefaultAccountNumber,
            };
        }

        throw new InvalidOperationException(
            "Brak credentials FedEx. Przekaż FedExAuthInfo w żądaniu (Scenariusz A) " +
            "lub skonfiguruj DefaultClientId, DefaultClientSecret i DefaultAccountNumber w FedExOptions (Scenariusz B).");
    }
}
