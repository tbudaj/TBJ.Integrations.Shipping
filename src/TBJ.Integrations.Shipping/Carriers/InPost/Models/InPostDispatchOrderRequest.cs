namespace TBJ.Integrations.Shipping.Carriers.InPost.Models;

/// <summary>
/// Żądanie utworzenia zlecenia odbioru (dispatch order) w InPost ShipX API.
/// </summary>
internal sealed class InPostDispatchOrderRequest
{
    /// <summary>Lista identyfikatorów przesyłek do odebrania.</summary>
    public List<string>? Shipments { get; set; }

    /// <summary>Adres, pod który ma przyjechać kurier.</summary>
    public InPostAddress? Address { get; set; }

    /// <summary>Komentarz / uwagi do zlecenia odbioru.</summary>
    public string? Comment { get; set; }
}
