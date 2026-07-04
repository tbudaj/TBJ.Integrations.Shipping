using System.Text.Json.Serialization;

namespace TBJ.Integrations.Shipping.Carriers.UPS.Models;

/// <summary>
/// Korzeń odpowiedzi UPS Tracking API.
/// </summary>
internal sealed class UpsTrackResponseRoot
{
    /// <summary>Odpowiedź śledzenia.</summary>
    public UpsTrackResponse? trackResponse { get; init; }
}

/// <summary>
/// Odpowiedź śledzenia UPS.
/// </summary>
internal sealed class UpsTrackResponse
{
    /// <summary>Lista przesyłek.</summary>
    public List<UpsTrackedShipment>? shipment { get; init; }
}

/// <summary>
/// Śledzona przesyłka UPS.
/// </summary>
internal sealed class UpsTrackedShipment
{
    /// <summary>Aktualny status przesyłki.</summary>
    public UpsTrackStatus? currentStatus { get; init; }

    /// <summary>Historia aktywności (zdarzeń).</summary>
    public List<UpsTrackActivity>? activity { get; init; }

    /// <summary>Szacowane daty doręczenia.</summary>
    public List<UpsDeliveryDate>? deliveryDate { get; init; }

    /// <summary>Informacje o doręczeniu (POD).</summary>
    public UpsDeliveryInformation? deliveryInformation { get; init; }
}

/// <summary>
/// Status przesyłki UPS.
/// </summary>
internal sealed class UpsTrackStatus
{
    /// <summary>Opis statusu.</summary>
    public string? description { get; init; }

    /// <summary>Kod statusu.</summary>
    public string? code { get; init; }

    /// <summary>Typ statusu.</summary>
    public string? type { get; init; }
}

/// <summary>
/// Zdarzenie śledzenia przesyłki UPS.
/// </summary>
internal sealed class UpsTrackActivity
{
    /// <summary>Lokalizacja zdarzenia.</summary>
    public UpsTrackLocation? location { get; init; }

    /// <summary>Status zdarzenia.</summary>
    public UpsTrackActivityStatus? status { get; init; }

    /// <summary>Data zdarzenia (format yyyyMMdd).</summary>
    public string? date { get; init; }

    /// <summary>Czas zdarzenia (format HHmmss).</summary>
    public string? time { get; init; }
}

/// <summary>
/// Lokalizacja zdarzenia UPS.
/// </summary>
internal sealed class UpsTrackLocation
{
    /// <summary>Adres lokalizacji.</summary>
    public UpsTrackAddress? address { get; init; }
}

/// <summary>
/// Adres lokalizacji zdarzenia UPS.
/// </summary>
internal sealed class UpsTrackAddress
{
    /// <summary>Pierwsza linia adresu.</summary>
    public string? addressLine1 { get; init; }

    /// <summary>Miasto.</summary>
    public string? city { get; init; }

    /// <summary>Kod kraju.</summary>
    public string? countryCode { get; init; }
}

/// <summary>
/// Status zdarzenia śledzenia UPS.
/// </summary>
internal sealed class UpsTrackActivityStatus
{
    /// <summary>Opis statusu zdarzenia.</summary>
    public string? description { get; init; }

    /// <summary>Typ statusu (D=Delivered, I=InTransit, O=OutForDelivery, P=PickedUp, X=AttemptFailed).</summary>
    public string? type { get; init; }

    /// <summary>Kod statusu zdarzenia.</summary>
    public string? code { get; init; }
}

/// <summary>
/// Szacowana data doręczenia UPS.
/// </summary>
internal sealed class UpsDeliveryDate
{
    /// <summary>Typ daty (np. "DEL" = delivery).</summary>
    public string? type { get; init; }

    /// <summary>Data w formacie yyyyMMdd.</summary>
    public string? date { get; init; }
}

/// <summary>
/// Informacje o doręczeniu przesyłki UPS (POD).
/// </summary>
internal sealed class UpsDeliveryInformation
{
    /// <summary>Osoba, która odebrała przesyłkę.</summary>
    public string? receivedBy { get; init; }

    /// <summary>Dokument POD.</summary>
    public UpsPod? pod { get; init; }
}

/// <summary>
/// Dokument Proof of Delivery (POD) UPS.
/// </summary>
internal sealed class UpsPod
{
    /// <summary>Zawartość dokumentu POD (HTML).</summary>
    public string? content { get; init; }
}
