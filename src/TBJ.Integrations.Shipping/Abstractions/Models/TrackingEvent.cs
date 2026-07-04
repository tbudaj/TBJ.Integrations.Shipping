namespace TBJ.Integrations.Shipping.Abstractions.Models;

using TBJ.Integrations.Shipping.Abstractions.Enums;

/// <summary>
/// Pojedyncze zdarzenie w historii śledzenia przesyłki.
/// </summary>
public sealed class TrackingEvent
{
    /// <summary>Data i czas zdarzenia (UTC).</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>Znormalizowany status przesyłki w chwili zdarzenia.</summary>
    public required ShipmentStatus Status { get; init; }

    /// <summary>Opis zdarzenia w języku zwróconym przez kuriera.</summary>
    public required string Description { get; init; }

    /// <summary>Lokalizacja zdarzenia (nazwa oddziału, miasta itp.) — opcjonalna.</summary>
    public string? Location { get; init; }
}
