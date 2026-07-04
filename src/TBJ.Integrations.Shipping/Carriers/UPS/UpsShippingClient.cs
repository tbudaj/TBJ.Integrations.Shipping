using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.UPS.Configuration;
using TBJ.Integrations.Shipping.Carriers.UPS.Internal;
using TBJ.Integrations.Shipping.Carriers.UPS.Mappers;
using TBJ.Integrations.Shipping.Carriers.UPS.Models;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.UPS;

/// <summary>
/// Implementacja adaptera kurierskiego dla UPS.
/// Komunikuje się z UPS REST API przez <see cref="UpsHttpClient"/>.
/// </summary>
internal sealed class UpsShippingClient : IShippingClient
{
    private readonly UpsHttpClient _http;
    private readonly UpsOptions _options;
    private readonly ILogger<UpsShippingClient> _logger;

    /// <summary>
    /// Inicjalizuje adapter UPS.
    /// </summary>
    /// <param name="http">Wewnętrzny klient HTTP UPS.</param>
    /// <param name="options">Opcje konfiguracyjne UPS.</param>
    /// <param name="logger">Logger.</param>
    public UpsShippingClient(UpsHttpClient http, UpsOptions options, ILogger<UpsShippingClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public CarrierType Carrier => CarrierType.UPS;

    /// <summary>
    /// Rozwiązuje credentials UPS na podstawie priorytetu:
    /// per-request <see cref="UpsAuthInfo"/> (Scenariusz A) → Default* z opcji (Scenariusz B).
    /// </summary>
    private UpsAuthInfo ResolveAuth(CarrierAuthInfo? authInfo)
    {
        if (authInfo is UpsAuthInfo upsAuth)
        {
            _logger.LogDebug("UPS: używam credentials tenanta (UpsAuthInfo per-request)");
            return upsAuth;
        }

        if (authInfo is not null)
            throw new InvalidOperationException(
                $"Nieprawidłowy typ AuthInfo dla UPS: oczekiwano {nameof(UpsAuthInfo)}, otrzymano {authInfo.GetType().Name}.");

        if (!string.IsNullOrWhiteSpace(_options.DefaultClientId) &&
            !string.IsNullOrWhiteSpace(_options.DefaultClientSecret) &&
            !string.IsNullOrWhiteSpace(_options.DefaultAccountNumber))
        {
            _logger.LogInformation("UPS: brak AuthInfo per-request — używam domyślnych credentials z UpsOptions (Scenariusz B)");
            return new UpsAuthInfo
            {
                ClientId      = _options.DefaultClientId,
                ClientSecret  = _options.DefaultClientSecret,
                AccountNumber = _options.DefaultAccountNumber,
            };
        }

        throw new InvalidOperationException(
            "Brak credentials UPS. Przekaż UpsAuthInfo w żądaniu (Scenariusz A) " +
            "lub skonfiguruj DefaultClientId, DefaultClientSecret i DefaultAccountNumber w UpsOptions (Scenariusz B).");
    }

    /// <inheritdoc />
    public async Task<ShipmentResult> CreateShipmentAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authInfo = ResolveAuth(request.AuthInfo);

        _logger.LogInformation("UPS: rejestracja nowej przesyłki");

        var upsRequest = UpsMapper.ToUpsShipRequest(request, authInfo);
        _logger.LogDebug("UPS: wysyłanie żądania rejestracji przesyłki");

        var responseJson = await _http.PostJsonAsync("/shipments/v2403/ship", upsRequest, authInfo, cancellationToken);

        var upsResponse = UpsJsonSerializer.Deserialize<UpsShipResponseRoot>(responseJson)
            ?? throw new ShippingException(
                "UPS API zwróciło pustą odpowiedź przy rejestracji przesyłki.",
                CarrierType.UPS);

        var result = UpsMapper.ToShipmentResult(upsResponse);

        _logger.LogInformation(
            "UPS: przesyłka zarejestrowana — TrackingNumber: {TrackingNumber}, ShipmentId: {ShipmentId}",
            result.TrackingNumber,
            result.CarrierShipmentId);

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
            "UPS: zamawianie odbioru dla {Count} przesyłek w dniu {PickupDate}",
            request.ShipmentIds.Count,
            request.PickupDate);

        var upsRequest = UpsMapper.ToUpsPickupRequest(request);
        var responseJson = await _http.PostJsonAsync("/pickups/v2205/pickup", upsRequest, authInfo, cancellationToken);

        var upsResponse = UpsJsonSerializer.Deserialize<UpsPickupResponseRoot>(responseJson)
            ?? throw new ShippingException(
                "UPS API zwróciło pustą odpowiedź przy zamawianiu odbioru.",
                CarrierType.UPS);

        var result = UpsMapper.ToPickupResult(upsResponse);

        _logger.LogInformation("UPS: odbiór zamówiony — PRN: {PickupOrderId}", result.PickupOrderId);

        return new PickupResult
        {
            PickupOrderId = result.PickupOrderId,
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

        var upsAuthInfo = ResolveAuth(authInfo);

        _logger.LogInformation("UPS: śledzenie przesyłki {TrackingNumber}", trackingNumber);

        var responseJson = await _http.GetAsync($"/track/v1/details/{trackingNumber}", upsAuthInfo, cancellationToken);

        var upsResponse = UpsJsonSerializer.Deserialize<UpsTrackResponseRoot>(responseJson)
            ?? throw new ShippingException(
                $"UPS API zwróciło pustą odpowiedź śledzenia dla {trackingNumber}.",
                CarrierType.UPS);

        var result = UpsMapper.ToTrackingResult(upsResponse, trackingNumber);

        _logger.LogInformation(
            "UPS: status przesyłki {TrackingNumber} — {Status}: {Description}",
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

        var upsAuthInfo = ResolveAuth(authInfo);

        _logger.LogInformation("UPS: pobieranie potwierdzenia doręczenia {TrackingNumber}", trackingNumber);

        var responseJson = await _http.GetAsync($"/track/v1/details/{trackingNumber}", upsAuthInfo, cancellationToken);

        var upsResponse = UpsJsonSerializer.Deserialize<UpsTrackResponseRoot>(responseJson)
            ?? throw new ShippingException(
                $"UPS API zwróciło pustą odpowiedź śledzenia dla {trackingNumber}.",
                CarrierType.UPS);

        var shipment = upsResponse.trackResponse?.shipment?.FirstOrDefault();
        var deliveredActivity = shipment?.activity?.FirstOrDefault(a => a.status?.type == "D");

        if (deliveredActivity == null)
        {
            throw new ShippingException(
                $"Przesyłka {trackingNumber} nie została jeszcze dostarczona.",
                CarrierType.UPS);
        }

        var result = UpsMapper.ToDeliveryConfirmationResult(upsResponse, trackingNumber);

        _logger.LogInformation(
            "UPS: potwierdzenie doręczenia {TrackingNumber} — {DeliveredAt}, odebrane przez: {ReceivedBy}",
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

}
