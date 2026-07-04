namespace TBJ.Integrations.Shipping.Carriers.FedEx.Models;

/// <summary>
/// Żądanie rejestracji przesyłki w FedEx Ship API.
/// </summary>
internal sealed class FedExShipRequest
{
    /// <summary>Dane przesyłki.</summary>
    public FedExRequestedShipment? RequestedShipment { get; init; }

    /// <summary>Numer konta FedEx.</summary>
    public FedExAccountNumber? AccountNumber { get; init; }
}

/// <summary>
/// Numer konta FedEx.
/// </summary>
internal sealed class FedExAccountNumber
{
    /// <summary>Wartość numeru konta.</summary>
    public string? Value { get; init; }
}

/// <summary>
/// Szczegóły żądanej przesyłki FedEx.
/// </summary>
internal sealed class FedExRequestedShipment
{
    /// <summary>Dane nadawcy przesyłki.</summary>
    public FedExShipParty? Shipper { get; init; }

    /// <summary>Lista odbiorców przesyłki.</summary>
    public List<FedExShipParty>? Recipients { get; init; }

    /// <summary>Typ odbioru (np. USE_SCHEDULED_PICKUP, DROPOFF_AT_FEDEX_LOCATION).</summary>
    public string? PickupType { get; init; }

    /// <summary>Typ usługi FedEx (np. INTERNATIONAL_ECONOMY, FEDEX_GROUND).</summary>
    public string? ServiceType { get; init; }

    /// <summary>Specyfikacja etykiety wysyłkowej.</summary>
    public FedExLabelSpecification? LabelSpecification { get; init; }

    /// <summary>Informacje o płatności za przesyłkę.</summary>
    public FedExShippingChargesPayment? ShippingChargesPayment { get; init; }

    /// <summary>Lista żądanych elementów paczek.</summary>
    public List<FedExRequestedPackage>? RequestedPackageLineItems { get; init; }
}

/// <summary>
/// Strona przesyłki FedEx (nadawca lub odbiorca).
/// </summary>
internal sealed class FedExShipParty
{
    /// <summary>Dane kontaktowe strony.</summary>
    public FedExContact? Contact { get; init; }

    /// <summary>Adres strony.</summary>
    public FedExShipAddress? Address { get; init; }
}

/// <summary>
/// Dane kontaktowe dla FedEx Ship API.
/// </summary>
internal sealed class FedExContact
{
    /// <summary>Imię i nazwisko osoby kontaktowej.</summary>
    public string? PersonName { get; init; }

    /// <summary>Numer telefonu.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Nazwa firmy.</summary>
    public string? CompanyName { get; init; }
}

/// <summary>
/// Adres dla FedEx Ship API.
/// </summary>
internal sealed class FedExShipAddress
{
    /// <summary>Linie adresowe (ulica, numer budynku).</summary>
    public List<string>? StreetLines { get; init; }

    /// <summary>Miasto.</summary>
    public string? City { get; init; }

    /// <summary>Kod pocztowy.</summary>
    public string? PostalCode { get; init; }

    /// <summary>Kod kraju (ISO 3166-1 alpha-2).</summary>
    public string? CountryCode { get; init; }
}

/// <summary>
/// Specyfikacja etykiety FedEx.
/// </summary>
internal sealed class FedExLabelSpecification
{
    /// <summary>Format etykiety (np. COMMON2D).</summary>
    public string LabelFormatType { get; init; } = "COMMON2D";

    /// <summary>Typ pliku etykiety (np. PDF).</summary>
    public string LabelFileType { get; init; } = "PDF";

    /// <summary>Typ obrazu etykiety (np. PDF).</summary>
    public string? ImageType { get; init; }
}

/// <summary>
/// Informacje o płatności za przesyłkę FedEx.
/// </summary>
internal sealed class FedExShippingChargesPayment
{
    /// <summary>Typ płatności (np. SENDER).</summary>
    public string? PaymentType { get; init; }

    /// <summary>Płatnik.</summary>
    public FedExPayor? Payor { get; init; }
}

/// <summary>
/// Płatnik za przesyłkę FedEx.
/// </summary>
internal sealed class FedExPayor
{
    /// <summary>Odpowiedzialna strona.</summary>
    public FedExResponsibleParty? ResponsibleParty { get; init; }
}

/// <summary>
/// Odpowiedzialna strona za płatność FedEx.
/// </summary>
internal sealed class FedExResponsibleParty
{
    /// <summary>Numer konta FedEx.</summary>
    public FedExAccountNumber? AccountNumber { get; init; }
}

/// <summary>
/// Żądana paczka w przesyłce FedEx.
/// </summary>
internal sealed class FedExRequestedPackage
{
    /// <summary>Waga paczki.</summary>
    public FedExWeight? Weight { get; init; }

    /// <summary>Wymiary paczki.</summary>
    public FedExDimensions? Dimensions { get; init; }
}

/// <summary>
/// Waga przesyłki FedEx.
/// </summary>
internal sealed class FedExWeight
{
    /// <summary>Jednostka wagi (KG lub LB).</summary>
    public string Units { get; init; } = "KG";

    /// <summary>Wartość wagi.</summary>
    public string? Value { get; init; }
}

/// <summary>
/// Wymiary paczki FedEx.
/// </summary>
internal sealed class FedExDimensions
{
    /// <summary>Długość paczki.</summary>
    public int? Length { get; init; }

    /// <summary>Szerokość paczki.</summary>
    public int? Width { get; init; }

    /// <summary>Wysokość paczki.</summary>
    public int? Height { get; init; }

    /// <summary>Jednostka wymiarów (CM lub IN).</summary>
    public string Units { get; init; } = "CM";
}
