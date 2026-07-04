namespace TBJ.Integrations.Shipping.Carriers.UPS.Models;

/// <summary>
/// Korzeń żądania zamówienia odbioru przesyłek przez UPS.
/// </summary>
internal sealed class UpsPickupRequestRoot
{
    /// <summary>Żądanie zamówienia odbioru.</summary>
    public UpsPickupCreationRequest? PickupCreationRequest { get; init; }
}

/// <summary>
/// Żądanie zamówienia odbioru przesyłek przez UPS.
/// </summary>
internal sealed class UpsPickupCreationRequest
{
    /// <summary>Informacje o dacie i oknie czasowym odbioru.</summary>
    public UpsPickupDateInfo? PickupDateInfo { get; init; }

    /// <summary>Nazwa wyświetlana klienta.</summary>
    public string? CustomerDisplayName { get; init; }

    /// <summary>Adres miejsca odbioru.</summary>
    public UpsPickupAddressInfo? PickupAddress { get; init; }
}

/// <summary>
/// Informacje o dacie i oknie czasowym odbioru UPS.
/// </summary>
internal sealed class UpsPickupDateInfo
{
    /// <summary>Godzina zakończenia możliwości odbioru (format HHmm).</summary>
    public string? CloseTime { get; init; }

    /// <summary>Godzina od kiedy kurier może odebrać (format HHmm).</summary>
    public string? ReadyTime { get; init; }

    /// <summary>Data odbioru (format yyyyMMdd).</summary>
    public string? PickupDate { get; init; }
}

/// <summary>
/// Adres miejsca odbioru UPS.
/// </summary>
internal sealed class UpsPickupAddressInfo
{
    /// <summary>Nazwa firmy w miejscu odbioru.</summary>
    public string? CompanyName { get; init; }

    /// <summary>Osoba kontaktowa.</summary>
    public string? ContactName { get; init; }

    /// <summary>Numer telefonu.</summary>
    public string? Phone { get; init; }

    /// <summary>Adres miejsca odbioru.</summary>
    public UpsAddress? Address { get; init; }
}
