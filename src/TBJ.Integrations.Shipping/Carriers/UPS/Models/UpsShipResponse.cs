namespace TBJ.Integrations.Shipping.Carriers.UPS.Models;

/// <summary>
/// Korzeń odpowiedzi UPS Shipping API przy rejestracji przesyłki.
/// </summary>
internal sealed class UpsShipResponseRoot
{
    /// <summary>Odpowiedź przesyłki.</summary>
    public UpsShipmentResponse? ShipmentResponse { get; init; }
}

/// <summary>
/// Odpowiedź operacji nadania przesyłki UPS.
/// </summary>
internal sealed class UpsShipmentResponse
{
    /// <summary>Wyniki nadania przesyłki.</summary>
    public UpsShipmentResults? ShipmentResults { get; init; }
}

/// <summary>
/// Wyniki nadania przesyłki UPS.
/// </summary>
internal sealed class UpsShipmentResults
{
    /// <summary>Identyfikator przesyłki UPS (główny numer śledzenia).</summary>
    public string? ShipmentIdentificationNumber { get; init; }

    /// <summary>Lista wyników dla poszczególnych paczek.</summary>
    public List<UpsPackageResult>? PackageResults { get; init; }
}

/// <summary>
/// Wynik paczki w przesyłce UPS.
/// </summary>
internal sealed class UpsPackageResult
{
    /// <summary>Numer śledzenia paczki.</summary>
    public string? TrackingNumber { get; init; }

    /// <summary>Etykieta przesyłki zakodowana w base64.</summary>
    public UpsShippingLabel? ShippingLabel { get; init; }
}

/// <summary>
/// Etykieta wysyłkowa UPS.
/// </summary>
internal sealed class UpsShippingLabel
{
    /// <summary>Obraz etykiety zakodowany w base64.</summary>
    public string? GraphicImage { get; init; }

    /// <summary>Format obrazu etykiety.</summary>
    public UpsImageFormat? ImageFormat { get; init; }
}

/// <summary>
/// Format obrazu etykiety UPS.
/// </summary>
internal sealed class UpsImageFormat
{
    /// <summary>Kod formatu (np. "GIF", "PDF").</summary>
    public string? Code { get; init; }
}
