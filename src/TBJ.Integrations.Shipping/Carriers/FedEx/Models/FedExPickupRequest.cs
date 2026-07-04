namespace TBJ.Integrations.Shipping.Carriers.FedEx.Models;

/// <summary>
/// Żądanie zamówienia odbioru przesyłek przez FedEx.
/// </summary>
internal sealed class FedExPickupRequest
{
    /// <summary>Informacje o miejscu i czasie odbioru.</summary>
    public FedExPickupOriginDetail? OriginDetail { get; init; }

    /// <summary>Kategoria usługi odbioru (np. FEDEX_EXPRESS).</summary>
    public string? PickupServiceCategory { get; init; }

    /// <summary>Numer konta FedEx do skojarzenia z dyspozycją odbioru.</summary>
    public FedExAccountNumber? AssociatedAccountNumber { get; init; }

    /// <summary>Kod przewoźnika FedEx (np. FDXE = FedEx Express, FDXG = FedEx Ground).</summary>
    public string? CarrierCode { get; init; }
}

/// <summary>
/// Szczegóły miejsca odbioru FedEx.
/// </summary>
internal sealed class FedExPickupOriginDetail
{
    /// <summary>Typ adresu odbioru (np. ACCOUNT).</summary>
    public string? PickupAddressType { get; init; }

    /// <summary>Lokalizacja odbioru.</summary>
    public FedExPickupLocation? PickupLocation { get; init; }

    /// <summary>Data i czas gotowości przesyłki do odbioru (ISO 8601).</summary>
    public string? ReadyDateTimestamp { get; init; }

    /// <summary>Godzina zamknięcia miejsca odbioru (format HH:mm).</summary>
    public string? CustomerCloseTime { get; init; }
}

/// <summary>
/// Lokalizacja odbioru FedEx.
/// </summary>
internal sealed class FedExPickupLocation
{
    /// <summary>Dane kontaktowe miejsca odbioru.</summary>
    public FedExContact? Contact { get; init; }

    /// <summary>Adres miejsca odbioru.</summary>
    public FedExShipAddress? Address { get; init; }
}
