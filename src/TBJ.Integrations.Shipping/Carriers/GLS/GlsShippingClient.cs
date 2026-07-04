using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.GLS.Configuration;
using TBJ.Integrations.Shipping.Carriers.GLS.Internal;
using TBJ.Integrations.Shipping.Carriers.GLS.Mappers;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.GLS;

/// <summary>
/// Implementacja adaptera kurierskiego dla GLS Polska.
/// Komunikuje się z GLS ADE-Plus WebAPI (SOAP) przez <see cref="GlsSoapClient"/>.
/// </summary>
internal sealed class GlsShippingClient : IShippingClient
{
    private readonly GlsSoapClient _soapClient;
    private readonly GlsOptions _options;
    private readonly ILogger<GlsShippingClient> _logger;

    /// <summary>
    /// Inicjalizuje adapter GLS.
    /// </summary>
    /// <param name="soapClient">Klient SOAP GLS.</param>
    /// <param name="options">Opcje konfiguracyjne GLS.</param>
    /// <param name="logger">Logger.</param>
    public GlsShippingClient(GlsSoapClient soapClient, GlsOptions options, ILogger<GlsShippingClient> logger)
    {
        _soapClient = soapClient;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public CarrierType Carrier => CarrierType.GLS;

    /// <summary>
    /// Rozwiązuje credentials GLS na podstawie priorytetu:
    /// per-request <see cref="GlsAuthInfo"/> (Scenariusz A) → Default* z opcji (Scenariusz B).
    /// </summary>
    private GlsAuthInfo ResolveAuth(CarrierAuthInfo? authInfo)
    {
        if (authInfo is GlsAuthInfo glsAuth)
        {
            _logger.LogDebug("GLS: używam credentials tenanta (GlsAuthInfo per-request)");
            return glsAuth;
        }

        if (authInfo is not null)
            throw new InvalidOperationException(
                $"Nieprawidłowy typ AuthInfo dla GLS: oczekiwano {nameof(GlsAuthInfo)}, otrzymano {authInfo.GetType().Name}.");

        if (!string.IsNullOrWhiteSpace(_options.DefaultUsername) &&
            !string.IsNullOrWhiteSpace(_options.DefaultPassword))
        {
            _logger.LogInformation("GLS: brak AuthInfo per-request — używam domyślnych credentials z GlsOptions (Scenariusz B)");
            return new GlsAuthInfo
            {
                Username = _options.DefaultUsername,
                Password = _options.DefaultPassword,
            };
        }

        throw new InvalidOperationException(
            "Brak credentials GLS. Przekaż GlsAuthInfo w żądaniu (Scenariusz A) " +
            "lub skonfiguruj DefaultUsername i DefaultPassword w GlsOptions (Scenariusz B).");
    }

    /// <inheritdoc />
    public async Task<ShipmentResult> CreateShipmentAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authInfo = ResolveAuth(request.AuthInfo);

        _logger.LogInformation("GLS: rejestracja nowej przesyłki");

        var consignmentXml = GlsMapper.BuildConsignmentXml(request, _options);
        _logger.LogDebug("GLS: consignment XML length {Length}", consignmentXml.Length);

        var responseXml = await _soapClient.PrepareConsignmentsAsync(consignmentXml, authInfo, cancellationToken);

        var consignmentId = GlsXmlHelper.ParseConsignmentId(responseXml);
        var trackingNumber = GlsXmlHelper.ParseTrackingNumber(responseXml);

        _logger.LogInformation(
            "GLS: przesyłka zarejestrowana — ConsignmentId: {ConsignmentId}, TrackingNumber: {TrackingNumber}",
            consignmentId,
            trackingNumber);

        byte[]? labelBytes = null;
        if (request.FetchLabel)
        {
            try
            {
                var labelResponseXml = await _soapClient.GetConsignLabelsAsync(consignmentId, authInfo, cancellationToken);
                labelBytes = GlsXmlHelper.ParseLabelBase64(labelResponseXml);
                _logger.LogDebug("GLS: etykieta pobrana — {Bytes} bajtów", labelBytes?.Length ?? 0);
            }
            catch (ShippingException ex)
            {
                _logger.LogDebug(ex, "GLS: nie udało się pobrać etykiety dla przesyłki {ConsignmentId}", consignmentId);
            }
        }

        var result = GlsMapper.ToShipmentResult(consignmentId, trackingNumber, labelBytes);
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
            "GLS: zamawianie odbioru dla {Count} przesyłek w dniu {PickupDate}",
            request.ShipmentIds.Count,
            request.PickupDate);

        var pickupXml = GlsMapper.BuildPickupXml(request, request.ShipmentIds, _options);
        _logger.LogDebug("GLS: pickup XML length {Length}", pickupXml.Length);

        var responseXml = await _soapClient.CallPickupAsync(pickupXml, authInfo, cancellationToken);
        var confirmationId = GlsXmlHelper.ParsePickupConfirmation(responseXml);

        _logger.LogInformation("GLS: odbiór zamówiony — ConfirmationId: {ConfirmationId}", confirmationId);

        return new PickupResult
        {
            PickupOrderId = confirmationId,
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

        var glsAuthInfo = ResolveAuth(authInfo);

        _logger.LogInformation("GLS: śledzenie przesyłki {TrackingNumber}", trackingNumber);

        var responseXml = await _soapClient.GetConsignStatusAsync(trackingNumber, glsAuthInfo, cancellationToken);
        var events = GlsXmlHelper.ParseConsignStatus(responseXml);

        var result = GlsMapper.ToTrackingResult(trackingNumber, events);

        _logger.LogInformation(
            "GLS: status przesyłki {TrackingNumber} — {Status}: {Description}",
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

        var glsAuthInfo = ResolveAuth(authInfo);

        _logger.LogInformation("GLS: pobieranie potwierdzenia doręczenia {TrackingNumber}", trackingNumber);

        var responseXml = await _soapClient.GetConsignStatusAsync(trackingNumber, glsAuthInfo, cancellationToken);
        var events = GlsXmlHelper.ParseConsignStatus(responseXml);

        var deliveredEvent = events.FirstOrDefault(e =>
            e.status.ToUpperInvariant() == "DELIVERED" ||
            e.status.ToUpperInvariant() == "DELIVERED");

        if (deliveredEvent == default)
        {
            // Sprawdzamy też inny wariant
            deliveredEvent = events.FirstOrDefault(e =>
                e.status.ToUpperInvariant().Contains("DELIV"));
        }

        if (deliveredEvent == default)
        {
            throw new ShippingException(
                $"Przesyłka {trackingNumber} nie została jeszcze dostarczona.",
                CarrierType.GLS);
        }

        _logger.LogInformation(
            "GLS: potwierdzenie doręczenia {TrackingNumber} — {DeliveredAt}",
            trackingNumber,
            deliveredEvent.time);

        return new DeliveryConfirmationResult
        {
            TrackingNumber = trackingNumber,
            DeliveredAt = deliveredEvent.time,
            ReceivedBy = null,
            RawCarrierResponse = responseXml,
        };
    }
}
