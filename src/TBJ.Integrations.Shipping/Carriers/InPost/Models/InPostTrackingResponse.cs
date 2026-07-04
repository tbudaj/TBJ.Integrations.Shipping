namespace TBJ.Integrations.Shipping.Carriers.InPost.Models;

/// <summary>
/// Odpowiedź InPost ShipX API z danymi śledzenia przesyłki.
/// </summary>
internal sealed class InPostTrackingResponse
{
    /// <summary>Numer śledzenia przesyłki.</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Aktualny status przesyłki w InPost.</summary>
    public string? Status { get; set; }

    /// <summary>Dodatkowe atrybuty specyficzne dla InPost.</summary>
    public Dictionary<string, string>? CustomAttributes { get; set; }

    /// <summary>Typ przesyłki (np. <c>parcel</c>, <c>letter</c>).</summary>
    public string? ShipmentType { get; set; }

    /// <summary>Data i czas utworzenia przesyłki w InPost.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Data i czas ostatniej aktualizacji.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Historia zdarzeń przesyłki.</summary>
    public List<InPostTrackingEvent>? Events { get; set; }
}

/// <summary>
/// Pojedyncze zdarzenie w historii śledzenia InPost.
/// </summary>
internal sealed class InPostTrackingEvent
{
    /// <summary>Data i czas wystąpienia zdarzenia.</summary>
    public DateTimeOffset? OccurredAt { get; set; }

    /// <summary>Status przesyłki w momencie zdarzenia.</summary>
    public string? Status { get; set; }

    /// <summary>Opis zdarzenia.</summary>
    public string? Description { get; set; }
}
