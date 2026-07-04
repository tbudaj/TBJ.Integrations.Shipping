using System.Net;
using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.DPD.Configuration;
using TBJ.Integrations.Shipping.Carriers.DPD.Internal;
using TBJ.Integrations.Shipping.Carriers.DPD.Mappers;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.DPD;

/// <summary>
/// Implementacja adaptera kurierskiego dla DPD Polska.
/// Komunikuje się z DPD Web Service (SOAP) przez <see cref="DpdSoapClient"/>.
/// </summary>
internal sealed class DpdShippingClient : IShippingClient
{
    private readonly DpdSoapClient _soapClient;
    private readonly DpdOptions _options;
    private readonly ILogger<DpdShippingClient> _logger;

    /// <summary>
    /// Inicjalizuje adapter DPD.
    /// </summary>
    /// <param name="soapClient">Klient SOAP DPD.</param>
    /// <param name="options">Opcje konfiguracyjne DPD.</param>
    /// <param name="logger">Logger.</param>
    public DpdShippingClient(DpdSoapClient soapClient, DpdOptions options, ILogger<DpdShippingClient> logger)
    {
        _soapClient = soapClient;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public CarrierType Carrier => CarrierType.DPD;

    /// <inheritdoc />
    public async Task<ShipmentResult> CreateShipmentAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authInfo = ResolveAuth(request.AuthInfo);

        _logger.LogInformation("DPD: rejestracja nowej przesyłki");

        var openUmlfXml = DpdMapper.BuildOpenUmlf(request);
        _logger.LogDebug("DPD: OpenUMLF XML length {Length}", openUmlfXml.Length);

        var responseXml = await _soapClient.GeneratePackagesNumbersAsync(openUmlfXml, authInfo, cancellationToken);

        var packageId = DpdXmlHelper.ParsePackageId(responseXml);
        var waybill = DpdXmlHelper.ParseWaybill(responseXml);

        _logger.LogInformation(
            "DPD: przesyłka zarejestrowana — PackageId: {PackageId}, Waybill: {Waybill}",
            packageId,
            waybill);

        byte[]? labelBytes = null;
        if (request.FetchLabel)
        {
            var pkgIdList = $"<ns:pkgIdList>{WebUtility.HtmlEncode(packageId)}</ns:pkgIdList>";
            var labelResponse = await _soapClient.GenerateLabelsAsync(pkgIdList, authInfo, cancellationToken);
            labelBytes = DpdXmlHelper.ParseLabelBase64(labelResponse);
            _logger.LogDebug("DPD: etykieta pobrana — {Bytes} bajtów", labelBytes?.Length ?? 0);
        }

        var result = DpdMapper.ToShipmentResult(packageId, waybill, labelBytes);
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
            "DPD: zamawianie odbioru dla {Count} przesyłek w dniu {PickupDate}",
            request.ShipmentIds.Count,
            request.PickupDate);

        var pickupXml = DpdMapper.BuildPickupXml(request, request.ShipmentIds, _options);
        _logger.LogDebug("DPD: pickup XML length {Length}", pickupXml.Length);

        var responseXml = await _soapClient.PackagesPickupCallAsync(pickupXml, authInfo, cancellationToken);
        var documentId = DpdXmlHelper.ParsePickupDocumentId(responseXml);

        _logger.LogInformation("DPD: odbiór zamówiony — DocumentId: {DocumentId}", documentId);

        return new PickupResult
        {
            PickupOrderId = documentId,
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

        var dpdAuthInfo = ResolveAuth(authInfo);

        _logger.LogInformation("DPD: śledzenie przesyłki {TrackingNumber}", trackingNumber);

        var responseXml = await _soapClient.FindPackageStatusAsync(trackingNumber, dpdAuthInfo, cancellationToken);
        var (state, description, eventTime) = DpdXmlHelper.ParseTrackingStatus(responseXml);

        _logger.LogInformation(
            "DPD: status przesyłki {TrackingNumber} — {State}: {Description}",
            trackingNumber,
            state,
            description);

        var result = DpdMapper.ToTrackingResult(trackingNumber, state, description, eventTime);
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

        var dpdAuthInfo = ResolveAuth(authInfo);

        _logger.LogInformation("DPD: pobieranie potwierdzenia doręczenia {TrackingNumber}", trackingNumber);

        var responseXml = await _soapClient.FindPackageStatusAsync(trackingNumber, dpdAuthInfo, cancellationToken);
        var (state, description, eventTime) = DpdXmlHelper.ParseTrackingStatus(responseXml);

        if (state?.ToUpperInvariant() != "D")
        {
            throw new ShippingException(
                $"Przesyłka {trackingNumber} nie została jeszcze dostarczona (status DPD: {state}).",
                CarrierType.DPD);
        }

        if (!eventTime.HasValue)
            throw new ShippingException(
                $"Brak daty doręczenia dla przesyłki {trackingNumber}.",
                CarrierType.DPD);

        _logger.LogInformation(
            "DPD: potwierdzenie doręczenia {TrackingNumber} — {DeliveredAt}",
            trackingNumber,
            eventTime.Value);

        var result = DpdMapper.ToDeliveryConfirmationResult(trackingNumber, description, eventTime.Value);
        return new DeliveryConfirmationResult
        {
            TrackingNumber = result.TrackingNumber,
            DeliveredAt = result.DeliveredAt,
            ReceivedBy = result.ReceivedBy,
            PodDocument = result.PodDocument,
            PodContentType = result.PodContentType,
            RawCarrierResponse = responseXml,
        };
    }

    /// <summary>
    /// Rozwiązuje credentials DPD na podstawie priorytetu:
    /// per-request <see cref="DpdAuthInfo"/> (Scenariusz A) → Default* z opcji (Scenariusz B).
    /// </summary>
    /// <param name="authInfo">Dane uwierzytelniające z żądania (opcjonalne).</param>
    /// <returns>Zrealizowane <see cref="DpdAuthInfo"/>.</returns>
    /// <exception cref="InvalidOperationException">Gdy brak credentials w obu źródłach.</exception>
    private DpdAuthInfo ResolveAuth(CarrierAuthInfo? authInfo)
    {
        if (authInfo is DpdAuthInfo dpdAuth)
        {
            // Scenariusz A: credentials tenanta przekazane per-request
            _logger.LogDebug("DPD: używam credentials tenanta (DpdAuthInfo per-request)");
            return dpdAuth;
        }

        if (authInfo is not null)
            throw new InvalidOperationException(
                $"Nieprawidłowy typ AuthInfo dla DPD: oczekiwano {nameof(DpdAuthInfo)}, otrzymano {authInfo.GetType().Name}.");

        // Scenariusz B: fallback na credentials naszego konta z appsettings
        if (!string.IsNullOrWhiteSpace(_options.DefaultUsername) &&
            !string.IsNullOrWhiteSpace(_options.DefaultPassword) &&
            _options.DefaultFid.HasValue)
        {
            _logger.LogInformation("DPD: brak AuthInfo per-request — używam domyślnych credentials z DpdOptions (Scenariusz B)");
            return new DpdAuthInfo
            {
                Username = _options.DefaultUsername,
                Password = _options.DefaultPassword,
                Fid      = _options.DefaultFid.Value,
            };
        }

        throw new InvalidOperationException(
            "Brak credentials DPD. Przekaż DpdAuthInfo w żądaniu (Scenariusz A) " +
            "lub skonfiguruj DefaultUsername, DefaultPassword i DefaultFid w DpdOptions (Scenariusz B).");
    }
}
