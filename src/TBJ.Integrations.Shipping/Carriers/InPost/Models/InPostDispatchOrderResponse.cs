namespace TBJ.Integrations.Shipping.Carriers.InPost.Models;

/// <summary>
/// Odpowiedź InPost ShipX API po utworzeniu zlecenia odbioru (dispatch order).
/// </summary>
internal sealed class InPostDispatchOrderResponse
{
    /// <summary>Identyfikator zlecenia odbioru nadany przez InPost.</summary>
    public long Id { get; set; }

    /// <summary>Status zlecenia odbioru.</summary>
    public string? Status { get; set; }

    /// <summary>Identyfikator zewnętrzny (opcjonalny).</summary>
    public string? ExternalId { get; set; }
}
