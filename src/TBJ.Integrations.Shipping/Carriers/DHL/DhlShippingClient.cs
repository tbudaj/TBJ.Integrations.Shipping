using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.DHL.Configuration;
using TBJ.Integrations.Shipping.Carriers.DHL.Internal;
using TBJ.Integrations.Shipping.Carriers.DHL.Mappers;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.DHL;

/// <summary>
/// Implementacja adaptera kurierskiego dla DHL24 Poland.
/// Komunikuje się z DHL WebAPI2 (SOAP) przez <see cref="DhlSoapClient"/>.
/// </summary>
internal sealed class DhlShippingClient : IShippingClient
{
    private readonly DhlSoapClient _soapClient;
    private readonly DhlOptions _options;
    private readonly ILogger<DhlShippingClient> _logger;

    /// <summary>
    /// Inicjalizuje adapter DHL.
    /// </summary>
    /// <param name="soapClient">Klient SOAP DHL.</param>
    /// <param name="options">Opcje konfiguracyjne DHL.</param>
    /// <param name="logger">Logger.</param>
    public DhlShippingClient(DhlSoapClient soapClient, DhlOptions options, ILogger<DhlShippingClient> logger)
    {
        _soapClient = soapClient;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public CarrierType Carrier => CarrierType.DHL;

    /// <summary>
    /// Rozwiązuje credentials DHL na podstawie priorytetu:
    /// per-request <see cref="DhlAuthInfo"/> (Scenariusz A) → Default* z opcji (Scenariusz B).
    /// </summary>
    private DhlAuthInfo ResolveAuth(CarrierAuthInfo? authInfo)
    {
        if (authInfo is DhlAuthInfo dhlAuth)
        {
            _logger.LogDebug("DHL: używam credentials tenanta (DhlAuthInfo per-request)");
            return dhlAuth;
        }

        if (authInfo is not null)
            throw new InvalidOperationException(
                $"Nieprawidłowy typ AuthInfo dla DHL: oczekiwano {nameof(DhlAuthInfo)}, otrzymano {authInfo.GetType().Name}.");

        if (!string.IsNullOrWhiteSpace(_options.DefaultLogin) &&
            !string.IsNullOrWhiteSpace(_options.DefaultPassword))
        {
            _logger.LogInformation("DHL: brak AuthInfo per-request — używam domyślnych credentials z DhlOptions (Scenariusz B)");
            return new DhlAuthInfo
            {
                Login    = _options.DefaultLogin,
                Password = _options.DefaultPassword,
            };
        }

        throw new InvalidOperationException(
            "Brak credentials DHL. Przekaż DhlAuthInfo w żądaniu (Scenariusz A) " +
            "lub skonfiguruj DefaultLogin i DefaultPassword w DhlOptions (Scenariusz B).");
    }

    /// <inheritdoc />
    public async Task<ShipmentResult> CreateShipmentAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authInfo = ResolveAuth(request.AuthInfo);

        _logger.LogInformation("DHL: rejestracja nowej przesyłki");

        var shipmentsXml = DhlMapper.BuildShipmentXml(request, _options);
        _logger.LogDebug("DHL: shipments XML length {Length}", shipmentsXml.Length);

        var responseXml = await _soapClient.CreateShipmentsAsync(shipmentsXml, authInfo, cancellationToken);

        var shipmentId = DhlXmlHelper.ParseShipmentId(responseXml);
        var labelId = DhlXmlHelper.ParseLabelId(responseXml);
        var labelBytes = DhlXmlHelper.ParseLabelContent(responseXml);

        _logger.LogInformation(
            "DHL: przesyłka zarejestrowana — ShipmentId: {ShipmentId}, LabelId: {LabelId}",
            shipmentId,
            labelId);

        var result = DhlMapper.ToShipmentResult(shipmentId, labelId, labelBytes);
        return new ShipmentResult
        {
            TrackingNumber = result.TrackingNumber,
            CarrierShipmentId = result.CarrierShipmentId,
            Label = result.Label,
            RawCarrierResponse = responseXml,
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
            "DHL: zamawianie kuriera dla {Count} przesyłek w dniu {PickupDate}",
            request.ShipmentIds.Count,
            request.PickupDate);

        var bookCourierXml = DhlMapper.BuildBookCourierXml(request, request.ShipmentIds);
        _logger.LogDebug("DHL: bookCourier XML length {Length}", bookCourierXml.Length);

        var responseXml = await _soapClient.BookCourierAsync(bookCourierXml, authInfo, cancellationToken);
        var confirmationId = DhlXmlHelper.ParseBookingConfirmation(responseXml);

        _logger.LogInformation("DHL: kurier zamówiony — ConfirmationId: {ConfirmationId}", confirmationId);

        var result = DhlMapper.ToPickupResult(confirmationId);
        return new PickupResult
        {
            PickupOrderId = result.PickupOrderId,
            RawCarrierResponse = responseXml,
        };
    }

    /// <inheritdoc />
    public async Task<TrackingResult> TrackShipmentAsync(
        CarrierAuthInfo? authInfo,
        string trackingNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        var dhlAuthInfo = ResolveAuth(authInfo);

        _logger.LogInformation("DHL: śledzenie przesyłki {TrackingNumber}", trackingNumber);

        var responseXml = await _soapClient.GetTrackAndTraceInfoAsync(trackingNumber, dhlAuthInfo, cancellationToken);
        var events = DhlXmlHelper.ParseTrackingEvents(responseXml);

        var result = DhlMapper.ToTrackingResult(trackingNumber, events);

        _logger.LogInformation(
            "DHL: status przesyłki {TrackingNumber} — {Status}: {Description}",
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
            RawCarrierResponse = responseXml,
        };
    }

    /// <inheritdoc />
    public async Task<DeliveryConfirmationResult> GetDeliveryConfirmationAsync(
        CarrierAuthInfo? authInfo,
        string trackingNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        var dhlAuthInfo = ResolveAuth(authInfo);

        _logger.LogInformation("DHL: pobieranie potwierdzenia doręczenia {TrackingNumber}", trackingNumber);

        var trackingXml = await _soapClient.GetTrackAndTraceInfoAsync(trackingNumber, dhlAuthInfo, cancellationToken);
        var events = DhlXmlHelper.ParseTrackingEvents(trackingXml);
        var deliveredEvent = events
            .Select(e => (e.status, e.description, e.timestamp, e.location))
            .FirstOrDefault(e => e.status?.ToLowerInvariant() == "delivered");

        if (deliveredEvent == default)
        {
            throw new ShippingException(
                $"Przesyłka {trackingNumber} nie została jeszcze dostarczona.",
                CarrierType.DHL);
        }

        var podResponseXml = await _soapClient.GetEpodAsync(trackingNumber, dhlAuthInfo, cancellationToken);
        var podBytes = DhlXmlHelper.ParsePodBytes(podResponseXml);
        var receivedBy = DhlXmlHelper.ParseReceivedBy(trackingXml);

        _logger.LogInformation(
            "DHL: potwierdzenie doręczenia {TrackingNumber} — {DeliveredAt}, odebrane przez: {ReceivedBy}",
            trackingNumber,
            deliveredEvent.timestamp,
            receivedBy);

        var result = DhlMapper.ToDeliveryConfirmationResult(
            trackingNumber,
            receivedBy ?? deliveredEvent.description,
            deliveredEvent.timestamp,
            podBytes);
        return new DeliveryConfirmationResult
        {
            TrackingNumber = result.TrackingNumber,
            DeliveredAt = result.DeliveredAt,
            ReceivedBy = result.ReceivedBy,
            PodDocument = result.PodDocument,
            PodContentType = result.PodContentType,
            RawCarrierResponse = trackingXml,
        };
    }
}
