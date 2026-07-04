namespace TBJ.Integrations.Shipping.Carriers.UPS.Models;

/// <summary>
/// Odpowiedź UPS API przy zamówieniu odbioru przesyłek.
/// </summary>
internal sealed class UpsPickupResponseRoot
{
    /// <summary>Wynik zamówienia odbioru.</summary>
    public UpsPickupCreationResponse? PickupCreationResponse { get; init; }
}

/// <summary>
/// Wynik zamówienia odbioru przez UPS.
/// </summary>
internal sealed class UpsPickupCreationResponse
{
    /// <summary>Numer referencyjny zamówienia odbioru (Pickup Reference Number).</summary>
    public string? PRN { get; init; }
}
