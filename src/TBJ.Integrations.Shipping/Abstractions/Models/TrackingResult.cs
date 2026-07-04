namespace TBJ.Integrations.Shipping.Abstractions.Models;

using TBJ.Integrations.Shipping.Abstractions.Enums;

/// <summary>
/// Wynik śledzenia przesyłki — aktualny status oraz historia zdarzeń.
/// </summary>
public sealed class TrackingResult
{
    /// <summary>Numer śledzenia przesyłki.</summary>
    public required string TrackingNumber { get; init; }

    /// <summary>Aktualny (ostatni) znormalizowany status przesyłki.</summary>
    public required ShipmentStatus CurrentStatus { get; init; }

    /// <summary>
    /// Opis ostatniego statusu w języku zwróconym przez kuriera.
    /// </summary>
    public required string StatusDescription { get; init; }

    /// <summary>
    /// Szacowana data doręczenia (jeśli dostarczona przez kuriera).
    /// </summary>
    public DateTimeOffset? EstimatedDelivery { get; init; }

    /// <summary>
    /// Pełna historia zdarzeń przesyłki (od najnowszego do najstarszego).
    /// </summary>
    public required IReadOnlyList<TrackingEvent> Events { get; init; }

    /// <summary>
    /// Surowe dane zwrócone przez kuriera (do celów diagnostycznych / audytu).
    /// </summary>
    public string? RawCarrierResponse { get; init; }
}
