namespace TBJ.Integrations.Shipping.Abstractions.Models;

/// <summary>
/// Wynik zamówienia kuriera po odbiór przesyłek.
/// </summary>
public sealed class PickupResult
{
    /// <summary>
    /// Identyfikator dyspozycji odbioru nadany przez kuriera.
    /// Może być używany do anulowania lub sprawdzenia statusu dyspozycji.
    /// </summary>
    public required string PickupOrderId { get; init; }

    /// <summary>Potwierdzony termin odbioru — data i okno czasowe (jeśli zwrócone przez kuriera).</summary>
    public string? ConfirmedPickupWindow { get; init; }

    /// <summary>
    /// Surowe dane zwrócone przez kuriera (do celów diagnostycznych / audytu).
    /// </summary>
    public string? RawCarrierResponse { get; init; }
}
