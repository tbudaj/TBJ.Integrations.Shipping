namespace TBJ.Integrations.Shipping.Carriers.InPost.Models;

/// <summary>
/// Odpowiedź InPost ShipX API po utworzeniu przesyłki.
/// </summary>
internal sealed class InPostShipmentResponse
{
    /// <summary>Identyfikator przesyłki nadany przez InPost.</summary>
    public long Id { get; set; }

    /// <summary>Status przesyłki w InPost (np. <c>created</c>, <c>confirmed</c>).</summary>
    public string? Status { get; set; }

    /// <summary>Numer śledzenia (tracking number).</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Identyfikator wybranej oferty (opcjonalny).</summary>
    public string? SelectedOffer { get; set; }

    /// <summary>Identyfikator formularza (opcjonalny).</summary>
    public string? FormId { get; set; }
}
