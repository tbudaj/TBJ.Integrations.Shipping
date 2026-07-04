using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Gateway;

/// <summary>
/// Implementacja bramy wysyłkowej (<see cref="IShippingGateway"/>), która deleguje
/// operacje do odpowiedniego adaptera kurierskiego na podstawie parametru <see cref="CarrierType"/>.
/// </summary>
internal sealed class ShippingGateway : IShippingGateway
{
    private readonly Dictionary<CarrierType, IShippingClient> _clients;
    private readonly ILogger<ShippingGateway> _logger;

    /// <summary>
    /// Inicjalizuje nową instancję bramy wysyłkowej.
    /// </summary>
    /// <param name="clients">Kolekcja zarejestrowanych adapterów kurierskich.</param>
    /// <param name="logger">Logger do rejestrowania operacji.</param>
    public ShippingGateway(IEnumerable<IShippingClient> clients, ILogger<ShippingGateway> logger)
    {
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _clients = new Dictionary<CarrierType, IShippingClient>();

        foreach (var client in clients)
        {
            _clients[client.Carrier] = client;
        }
    }

    /// <inheritdoc />
    public IShippingClient GetClient(CarrierType carrier)
    {
        if (!_clients.TryGetValue(carrier, out var client))
        {
            throw new InvalidOperationException(
                $"Kurier {carrier} nie jest zarejestrowany w Shipping Gateway. " +
                $"Upewnij się, że wywołano Add{carrier}() w konfiguracji.");
        }

        return client;
    }

    /// <inheritdoc />
    public async Task<ShipmentResult> CreateShipmentAsync(
        CarrierType carrier,
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var client = GetClient(carrier);
        _logger.LogInformation("ShippingGateway: [{Carrier}] CreateShipmentAsync", carrier);

        var result = await client.CreateShipmentAsync(request, cancellationToken);

        _logger.LogDebug(
            "ShippingGateway: [{Carrier}] CreateShipmentAsync zakończona — TrackingNumber: {TrackingNumber}",
            carrier,
            result.TrackingNumber);

        return result;
    }

    /// <inheritdoc />
    public async Task<PickupResult> OrderPickupAsync(
        CarrierType carrier,
        OrderPickupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var client = GetClient(carrier);
        _logger.LogInformation("ShippingGateway: [{Carrier}] OrderPickupAsync", carrier);

        var result = await client.OrderPickupAsync(request, cancellationToken);

        _logger.LogDebug(
            "ShippingGateway: [{Carrier}] OrderPickupAsync zakończona — PickupOrderId: {PickupOrderId}",
            carrier,
            result.PickupOrderId);

        return result;
    }

    /// <inheritdoc />
    public async Task<TrackingResult> TrackShipmentAsync(
        CarrierType carrier,
        CarrierAuthInfo? authInfo,
        string trackingNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        var client = GetClient(carrier);

        // authInfo może być null — adapter sam rozwiąże credentials (Scenariusz B: Default* z opcji)
        _logger.LogInformation(
            "ShippingGateway: [{Carrier}] TrackShipmentAsync — {AuthMode}",
            carrier,
            authInfo is not null ? "credentials per-request" : "domyślne credentials z opcji");

        var result = await client.TrackShipmentAsync(authInfo, trackingNumber, cancellationToken);

        _logger.LogDebug(
            "ShippingGateway: [{Carrier}] TrackShipmentAsync zakończona — TrackingNumber: {TrackingNumber}, Status: {Status}",
            carrier,
            result.TrackingNumber,
            result.CurrentStatus);

        return result;
    }

    /// <inheritdoc />
    public async Task<DeliveryConfirmationResult> GetDeliveryConfirmationAsync(
        CarrierType carrier,
        CarrierAuthInfo? authInfo,
        string trackingNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        var client = GetClient(carrier);

        // authInfo może być null — adapter sam rozwiąże credentials (Scenariusz B: Default* z opcji)
        _logger.LogInformation(
            "ShippingGateway: [{Carrier}] GetDeliveryConfirmationAsync — {AuthMode}",
            carrier,
            authInfo is not null ? "credentials per-request" : "domyślne credentials z opcji");

        var result = await client.GetDeliveryConfirmationAsync(authInfo, trackingNumber, cancellationToken);

        _logger.LogDebug(
            "ShippingGateway: [{Carrier}] GetDeliveryConfirmationAsync zakończona — TrackingNumber: {TrackingNumber}, DeliveredAt: {DeliveredAt}",
            carrier,
            result.TrackingNumber,
            result.DeliveredAt);

        return result;
    }
}
