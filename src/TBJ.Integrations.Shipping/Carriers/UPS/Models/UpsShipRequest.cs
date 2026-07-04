namespace TBJ.Integrations.Shipping.Carriers.UPS.Models;

/// <summary>
/// Korzeń żądania rejestracji przesyłki w UPS Shipping API.
/// </summary>
internal sealed class UpsShipRequestRoot
{
    /// <summary>Żądanie przesyłki.</summary>
    public UpsShipmentRequest? ShipmentRequest { get; init; }
}

/// <summary>
/// Żądanie przesyłki UPS.
/// </summary>
internal sealed class UpsShipmentRequest
{
    /// <summary>Dane przesyłki.</summary>
    public UpsShipment? Shipment { get; init; }
}

/// <summary>
/// Dane przesyłki UPS.
/// </summary>
internal sealed class UpsShipment
{
    /// <summary>Dane nadawcy.</summary>
    public UpsShipper? Shipper { get; init; }

    /// <summary>Dane odbiorcy (ShipTo).</summary>
    public UpsParty? ShipTo { get; init; }

    /// <summary>Dane nadawcy (ShipFrom).</summary>
    public UpsParty? ShipFrom { get; init; }

    /// <summary>Usługa UPS.</summary>
    public UpsService? Service { get; init; }

    /// <summary>Lista paczek w przesyłce.</summary>
    public List<UpsPackage>? Package { get; init; }

    /// <summary>Informacje o płatności.</summary>
    public UpsPaymentInformation? PaymentInformation { get; init; }
}

/// <summary>
/// Dane nadawcy (shipper) przesyłki UPS.
/// </summary>
internal sealed class UpsShipper
{
    /// <summary>Nazwa nadawcy.</summary>
    public string? Name { get; init; }

    /// <summary>Osoba kontaktowa.</summary>
    public string? AttentionName { get; init; }

    /// <summary>Numer konta UPS nadawcy.</summary>
    public string? ShipperNumber { get; init; }

    /// <summary>Telefon nadawcy.</summary>
    public UpsPhone? Phone { get; init; }

    /// <summary>Adres nadawcy.</summary>
    public UpsAddress? Address { get; init; }
}

/// <summary>
/// Dane strony przesyłki UPS (odbiorca lub nadawca ShipFrom).
/// </summary>
internal sealed class UpsParty
{
    /// <summary>Nazwa firmy lub osoby.</summary>
    public string? Name { get; init; }

    /// <summary>Osoba kontaktowa.</summary>
    public string? AttentionName { get; init; }

    /// <summary>Telefon kontaktowy.</summary>
    public UpsPhone? Phone { get; init; }

    /// <summary>Adres dostawy lub odbioru.</summary>
    public UpsAddress? Address { get; init; }
}

/// <summary>
/// Adres UPS.
/// </summary>
internal sealed class UpsAddress
{
    /// <summary>Linie adresowe (ulica, numer budynku).</summary>
    public List<string>? AddressLine { get; init; }

    /// <summary>Miasto.</summary>
    public string? City { get; init; }

    /// <summary>Kod stanu/województwa (wymagany dla USA).</summary>
    public string? StateProvinceCode { get; init; }

    /// <summary>Kod pocztowy.</summary>
    public string? PostalCode { get; init; }

    /// <summary>Kod kraju (ISO 3166-1 alpha-2).</summary>
    public string? CountryCode { get; init; }
}

/// <summary>
/// Numer telefonu UPS.
/// </summary>
internal sealed class UpsPhone
{
    /// <summary>Numer telefonu.</summary>
    public string? Number { get; init; }
}

/// <summary>
/// Usługa UPS (kod usługi).
/// </summary>
internal sealed class UpsService
{
    /// <summary>Kod usługi UPS (np. "03" = UPS Ground, "11" = UPS Standard).</summary>
    public string? Code { get; init; }
}

/// <summary>
/// Paczka w przesyłce UPS.
/// </summary>
internal sealed class UpsPackage
{
    /// <summary>Typ opakowania (domyślnie "02" = Customer Supplied Package).</summary>
    public UpsPackagingType? Packaging { get; init; }

    /// <summary>Waga paczki.</summary>
    public UpsPackageWeight? PackageWeight { get; init; }
}

/// <summary>
/// Typ opakowania UPS.
/// </summary>
internal sealed class UpsPackagingType
{
    /// <summary>Kod opakowania.</summary>
    public string Code { get; init; } = "02";
}

/// <summary>
/// Waga paczki UPS.
/// </summary>
internal sealed class UpsPackageWeight
{
    /// <summary>Jednostka miary wagi.</summary>
    public UpsUnitOfMeasurement? UnitOfMeasurement { get; init; }

    /// <summary>Wartość wagi.</summary>
    public string? Weight { get; init; }
}

/// <summary>
/// Jednostka miary UPS.
/// </summary>
internal sealed class UpsUnitOfMeasurement
{
    /// <summary>Kod jednostki (np. "KGS" = kilogramy, "LBS" = funty).</summary>
    public string Code { get; init; } = "KGS";
}

/// <summary>
/// Informacje o płatności za przesyłkę UPS.
/// </summary>
internal sealed class UpsPaymentInformation
{
    /// <summary>Lista opłat za przesyłkę.</summary>
    public List<UpsShipmentCharge>? ShipmentCharge { get; init; }
}

/// <summary>
/// Opłata za przesyłkę UPS.
/// </summary>
internal sealed class UpsShipmentCharge
{
    /// <summary>Typ opłaty ("01" = Transportation, "02" = Duties and Taxes).</summary>
    public string? Type { get; init; }

    /// <summary>Płatnik — rachunek nadawcy.</summary>
    public UpsBillShipper? BillShipper { get; init; }
}

/// <summary>
/// Dane rachunku nadawcy do rozliczenia przesyłki.
/// </summary>
internal sealed class UpsBillShipper
{
    /// <summary>Numer konta UPS nadawcy.</summary>
    public string? AccountNumber { get; init; }
}
