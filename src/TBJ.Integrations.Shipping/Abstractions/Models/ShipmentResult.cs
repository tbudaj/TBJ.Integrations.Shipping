namespace TBJ.Integrations.Shipping.Abstractions.Models;

/// <summary>
/// Wynik rejestracji przesyłki u kuriera.
/// </summary>
public sealed class ShipmentResult
{
    /// <summary>
    /// Numer przesyłki do śledzenia (tracking number).
    /// Używany w metodzie <see cref="Interfaces.IShippingClient.TrackShipmentAsync"/>.
    /// </summary>
    public required string TrackingNumber { get; init; }

    /// <summary>
    /// Identyfikator przesyłki nadany przez kuriera (ID wewnętrzne).
    /// Może być używany do zamawiania kuriera lub generowania protokołów (np. DPD, DHL).
    /// </summary>
    public required string CarrierShipmentId { get; init; }

    /// <summary>
    /// Etykieta wysyłkowa. Null gdy <see cref="CreateShipmentRequest.FetchLabel"/> było false
    /// lub kurier wymaga osobnego kroku generowania etykiety.
    /// </summary>
    public LabelResult? Label { get; init; }

    /// <summary>
    /// Surowe dane zwrócone przez kuriera (do celów diagnostycznych / audytu).
    /// </summary>
    public string? RawCarrierResponse { get; init; }
}
