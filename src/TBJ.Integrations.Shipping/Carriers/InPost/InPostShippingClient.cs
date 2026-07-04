using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.InPost.Configuration;
using TBJ.Integrations.Shipping.Carriers.InPost.Internal;
using TBJ.Integrations.Shipping.Carriers.InPost.Mappers;
using TBJ.Integrations.Shipping.Carriers.InPost.Models;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.InPost;

/// <summary>
/// Wspólna baza dla obu klientów InPost (Paczkomat i Kurier).
/// Zawiera logikę HTTP, auth oraz operacje niezależne od trybu dostawy.
/// </summary>
internal abstract class InPostShippingClientBase : IShippingClient
{
    private readonly InPostHttpClient _http;
    private readonly InPostOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Inicjalizuje wspólną bazę klienta InPost.
    /// </summary>
    /// <param name="http">Wewnętrzny klient HTTP InPost.</param>
    /// <param name="options">Opcje konfiguracyjne InPost.</param>
    /// <param name="logger">Logger.</param>
    protected InPostShippingClientBase(InPostHttpClient http, InPostOptions options, ILogger logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public abstract CarrierType Carrier { get; }

    /// <summary>Czytelna nazwa trybu dostawy — używana w logach.</summary>
    protected abstract string DeliveryModeName { get; }

    /// <inheritdoc />
    public async Task<ShipmentResult> CreateShipmentAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var authInfo = ResolveAuth(request.AuthInfo);

        _logger.LogInformation("InPost [{Mode}]: CreateShipmentAsync — rejestracja przesyłki", DeliveryModeName);

        var inPostRequest = InPostMapper.ToInPostRequest(request);
        var endpoint = $"/v1/organizations/{authInfo.OrganizationId}/shipments";

        var responseJson = await _http.PostJsonAsync(endpoint, inPostRequest, authInfo, cancellationToken, Carrier);
        var shipmentResponse = InPostJsonSerializer.Deserialize<InPostShipmentResponse>(responseJson)
            ?? throw new ShippingException(
                "InPost API zwróciło pustą odpowiedź przy rejestracji przesyłki.",
                Carrier);

        _logger.LogDebug(
            "InPost [{Mode}]: przesyłka utworzona — Id: {ShipmentId}, TrackingNumber: {TrackingNumber}",
            DeliveryModeName,
            shipmentResponse.Id,
            shipmentResponse.TrackingNumber);

        LabelResult? label = null;
        if (request.FetchLabel)
        {
            label = await FetchLabelAsync(shipmentResponse.Id, authInfo, cancellationToken);
        }

        return InPostMapper.ToShipmentResult(shipmentResponse, label);
    }

    /// <inheritdoc />
    public async Task<PickupResult> OrderPickupAsync(
        OrderPickupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authInfo = ResolveAuth(request.AuthInfo);

        _logger.LogInformation(
            "InPost [{Mode}]: OrderPickupAsync — zamawianie odbioru dla {Count} przesyłek",
            DeliveryModeName,
            request.ShipmentIds.Count);

        var dispatchRequest = InPostMapper.ToDispatchOrderRequest(request, request.ShipmentIds);
        var endpoint = $"/v1/organizations/{authInfo.OrganizationId}/dispatch_orders";

        var responseJson = await _http.PostJsonAsync(endpoint, dispatchRequest, authInfo, cancellationToken, Carrier);
        var dispatchResponse = InPostJsonSerializer.Deserialize<InPostDispatchOrderResponse>(responseJson)
            ?? throw new ShippingException(
                "InPost API zwróciło pustą odpowiedź przy zamawianiu odbioru.",
                Carrier);

        _logger.LogDebug(
            "InPost [{Mode}]: zlecenie odbioru utworzone — Id: {DispatchOrderId}, Status: {Status}",
            DeliveryModeName,
            dispatchResponse.Id,
            dispatchResponse.Status);

        return InPostMapper.ToPickupResult(dispatchResponse);
    }

    /// <inheritdoc />
    public async Task<TrackingResult> TrackShipmentAsync(
        CarrierAuthInfo? authInfo,
        string trackingNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        var inPostAuthInfo = ResolveAuth(authInfo);

        _logger.LogInformation(
            "InPost [{Mode}]: TrackShipmentAsync — śledzenie przesyłki {TrackingNumber}",
            DeliveryModeName,
            trackingNumber);

        var endpoint = $"/v1/tracking/{trackingNumber}";
        var responseJson = await _http.GetAsync(endpoint, inPostAuthInfo, cancellationToken, Carrier);
        var trackingResponse = InPostJsonSerializer.Deserialize<InPostTrackingResponse>(responseJson)
            ?? throw new ShippingException(
                $"InPost API zwróciło pustą odpowiedź śledzenia dla {trackingNumber}.",
                Carrier);

        _logger.LogDebug(
            "InPost [{Mode}]: wynik śledzenia — TrackingNumber: {TrackingNumber}, Status: {Status}",
            DeliveryModeName,
            trackingResponse.TrackingNumber,
            trackingResponse.Status);

        return InPostMapper.ToTrackingResult(trackingResponse);
    }

    /// <inheritdoc />
    public async Task<DeliveryConfirmationResult> GetDeliveryConfirmationAsync(
        CarrierAuthInfo? authInfo,
        string trackingNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        var inPostAuthInfo = ResolveAuth(authInfo);

        _logger.LogInformation(
            "InPost [{Mode}]: GetDeliveryConfirmationAsync — potwierdzenie doręczenia {TrackingNumber}",
            DeliveryModeName,
            trackingNumber);

        var endpoint = $"/v1/tracking/{trackingNumber}";
        var responseJson = await _http.GetAsync(endpoint, inPostAuthInfo, cancellationToken, Carrier);
        var trackingResponse = InPostJsonSerializer.Deserialize<InPostTrackingResponse>(responseJson)
            ?? throw new ShippingException(
                $"InPost API zwróciło pustą odpowiedź śledzenia dla {trackingNumber}.",
                Carrier);

        var status = InPostMapper.MapInPostStatus(trackingResponse.Status);
        if (status != Abstractions.Enums.ShipmentStatus.Delivered)
        {
            throw new ShippingException(
                $"Przesyłka {trackingNumber} nie została jeszcze dostarczona. Aktualny status: {trackingResponse.Status}.",
                Carrier);
        }

        _logger.LogDebug(
            "InPost [{Mode}]: potwierdzenie doręczenia — TrackingNumber: {TrackingNumber}, Status: {Status}",
            DeliveryModeName,
            trackingResponse.TrackingNumber,
            trackingResponse.Status);

        return InPostMapper.ToDeliveryConfirmationResult(trackingResponse);
    }

    /// <summary>
    /// Walidacja żądania specyficzna dla trybu dostawy (implementowana w klasach pochodnych).
    /// </summary>
    /// <param name="request">Żądanie rejestracji przesyłki.</param>
    protected abstract void ValidateRequest(CreateShipmentRequest request);

    /// <summary>
    /// Rozwiązuje credentials InPost na podstawie priorytetu:
    /// per-request <see cref="InPostAuthInfo"/> (Scenariusz A) → Default* z opcji (Scenariusz B).
    /// </summary>
    /// <param name="authInfo">Dane uwierzytelniające z żądania (opcjonalne).</param>
    /// <returns>Zrealizowane <see cref="InPostAuthInfo"/>.</returns>
    /// <exception cref="InvalidOperationException">Gdy brak credentials w obu źródłach.</exception>
    protected InPostAuthInfo ResolveAuth(CarrierAuthInfo? authInfo)
    {
        if (authInfo is InPostAuthInfo inPostAuth)
        {
            // Scenariusz A: credentials tenanta przekazane per-request
            _logger.LogDebug("InPost [{Mode}]: używam credentials tenanta (InPostAuthInfo per-request)", DeliveryModeName);
            return inPostAuth;
        }

        if (authInfo is not null)
            throw new InvalidOperationException(
                $"Nieprawidłowy typ AuthInfo dla InPost: oczekiwano {nameof(InPostAuthInfo)}, otrzymano {authInfo.GetType().Name}.");

        // Scenariusz B: fallback na credentials naszego konta z appsettings
        if (!string.IsNullOrWhiteSpace(_options.DefaultAccessToken) &&
            !string.IsNullOrWhiteSpace(_options.DefaultOrganizationId))
        {
            _logger.LogInformation(
                "InPost [{Mode}]: brak AuthInfo per-request — używam domyślnych credentials z InPostOptions (Scenariusz B)",
                DeliveryModeName);
            return new InPostAuthInfo
            {
                AccessToken    = _options.DefaultAccessToken,
                OrganizationId = _options.DefaultOrganizationId,
            };
        }

        throw new InvalidOperationException(
            "Brak credentials InPost. Przekaż InPostAuthInfo w żądaniu (Scenariusz A) " +
            "lub skonfiguruj DefaultAccessToken i DefaultOrganizationId w InPostOptions (Scenariusz B).");
    }

    /// <summary>
    /// Pobiera etykietę przesyłki z InPost ShipX API.
    /// </summary>
    /// <param name="shipmentId">Identyfikator przesyłki InPost.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Wynik etykiety lub <c>null</c> w razie błędu.</returns>
    private async Task<LabelResult?> FetchLabelAsync(long shipmentId, InPostAuthInfo authInfo, CancellationToken cancellationToken)
    {
        try
        {
            var labelEndpoint = $"/v1/organizations/{authInfo.OrganizationId}/shipments/{shipmentId}/label";
            if (!string.IsNullOrWhiteSpace(_options.LabelFormat))
            {
                labelEndpoint += $"?format={_options.LabelFormat}";
            }

            _logger.LogDebug("InPost [{Mode}]: pobieranie etykiety dla przesyłki {ShipmentId}", DeliveryModeName, shipmentId);

            var (content, contentType) = await _http.GetBytesAsync(labelEndpoint, authInfo, cancellationToken, Carrier);

            return new LabelResult
            {
                Content = content,
                ContentType = contentType,
                FileName = $"label_{shipmentId}.{_options.LabelFormat}",
            };
        }
        catch (ShippingException ex)
        {
            _logger.LogDebug(
                ex,
                "InPost [{Mode}]: nie udało się pobrać etykiety dla przesyłki {ShipmentId} — {Message}",
                DeliveryModeName,
                shipmentId,
                ex.Message);
            return null;
        }
    }
}

/// <summary>
/// Klient InPost dla dostawy do Paczkomatu (<see cref="CarrierType.InPostLocker"/>).
/// Wymaga podania <see cref="CreateShipmentRequest.LockerTargetMachineId"/> w żądaniu.
/// Domyślna usługa: <c>inpost_locker_standard</c>.
/// </summary>
internal sealed class InPostLockerShippingClient : InPostShippingClientBase
{
    /// <summary>
    /// Inicjalizuje klienta InPost Paczkomat.
    /// </summary>
    /// <param name="http">Wewnętrzny klient HTTP InPost.</param>
    /// <param name="options">Opcje konfiguracyjne InPost.</param>
    /// <param name="logger">Logger.</param>
    public InPostLockerShippingClient(
        InPostHttpClient http,
        InPostOptions options,
        ILogger<InPostLockerShippingClient> logger)
        : base(http, options, logger) { }

    /// <inheritdoc />
    public override CarrierType Carrier => CarrierType.InPostLocker;

    /// <inheritdoc />
    protected override string DeliveryModeName => "Paczkomat";

    /// <inheritdoc />
    protected override void ValidateRequest(CreateShipmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LockerTargetMachineId))
            throw new InvalidOperationException(
                "Pole LockerTargetMachineId jest wymagane dla dostawy do Paczkomatu (CarrierType.InPostLocker). " +
                "Podaj identyfikator paczkomatu docelowego, np. \"WAW123M\".");
    }
}

/// <summary>
/// Klient InPost dla dostawy kurierem door-to-door (<see cref="CarrierType.InPostCourier"/>).
/// Domyślna usługa: <c>inpost_courier_standard</c>.
/// </summary>
internal sealed class InPostCourierShippingClient : InPostShippingClientBase
{
    /// <summary>
    /// Inicjalizuje klienta InPost Kurier.
    /// </summary>
    /// <param name="http">Wewnętrzny klient HTTP InPost.</param>
    /// <param name="options">Opcje konfiguracyjne InPost.</param>
    /// <param name="logger">Logger.</param>
    public InPostCourierShippingClient(
        InPostHttpClient http,
        InPostOptions options,
        ILogger<InPostCourierShippingClient> logger)
        : base(http, options, logger) { }

    /// <inheritdoc />
    public override CarrierType Carrier => CarrierType.InPostCourier;

    /// <inheritdoc />
    protected override string DeliveryModeName => "Kurier";

    /// <inheritdoc />
    protected override void ValidateRequest(CreateShipmentRequest request)
    {
        // Brak dodatkowej walidacji specyficznej dla trybu kurierskiego.
        // ServiceCode może być null — InPost ShipX API użyje inpost_courier_standard jako domyślny.
    }
}
