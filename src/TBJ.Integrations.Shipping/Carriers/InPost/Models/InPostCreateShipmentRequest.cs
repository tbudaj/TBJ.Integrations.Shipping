namespace TBJ.Integrations.Shipping.Carriers.InPost.Models;

/// <summary>
/// Żądanie utworzenia przesyłki w InPost ShipX API.
/// Mapowane z <see cref="Abstractions.Models.CreateShipmentRequest"/>.
/// </summary>
internal sealed class InPostCreateShipmentRequest
{
    /// <summary>Dane odbiorcy przesyłki.</summary>
    public InPostAddress? Receiver { get; set; }

    /// <summary>Dane nadawcy przesyłki.</summary>
    public InPostAddress? Sender { get; set; }

    /// <summary>Lista paczek wchodzących w skład przesyłki.</summary>
    public List<InPostParcel>? Parcels { get; set; }

    /// <summary>Kod usługi (np. <c>inpost_courier_standard</c>, <c>inpost_locker_standard</c>).</summary>
    public string? Service { get; set; }

    /// <summary>Referencja własna nadawcy.</summary>
    public string? Reference { get; set; }

    /// <summary>Konfiguracja pobrania (COD) — opcjonalna.</summary>
    public InPostCod? Cod { get; set; }

    /// <summary>Konfiguracja ubezpieczenia — opcjonalna.</summary>
    public InPostInsurance? Insurance { get; set; }

    /// <summary>Komentarz / uwagi do przesyłki.</summary>
    public string? Comments { get; set; }

    /// <summary>Dodatkowe atrybuty specyficzne dla InPost.</summary>
    public Dictionary<string, string>? CustomAttributes { get; set; }
}

/// <summary>
/// Konfiguracja pobrania (COD) w formacie InPost ShipX API.
/// </summary>
internal sealed class InPostCod
{
    /// <summary>Kwota pobrania.</summary>
    public decimal Amount { get; set; }

    /// <summary>Waluta (domyślnie: PLN).</summary>
    public string Currency { get; set; } = "PLN";
}

/// <summary>
/// Konfiguracja ubezpieczenia w formacie InPost ShipX API.
/// </summary>
internal sealed class InPostInsurance
{
    /// <summary>Zadeklarowana wartość przesyłki.</summary>
    public decimal Amount { get; set; }

    /// <summary>Waluta (domyślnie: PLN).</summary>
    public string Currency { get; set; } = "PLN";
}
