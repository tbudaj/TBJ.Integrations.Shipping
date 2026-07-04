using TBJ.Integrations.Shipping.Abstractions.Auth;

namespace TBJ.Integrations.Shipping.Abstractions.Models;

/// <summary>
/// Żądanie rejestracji nowej przesyłki u kuriera.
/// Zawiera dane nadawcy, odbiorcy, paczki oraz usług dodatkowych.
/// </summary>
public sealed class CreateShipmentRequest
{
    /// <summary>
    /// Dane uwierzytelniające do API kuriera (opcjonalne).
    /// <para>
    /// Gdy podane — używane są credentials tenanta (Scenariusz A: konto tenanta).
    /// Gdy <c>null</c> — adapter używa domyślnych credentials z <c>appsettings.json</c>
    /// (Scenariusz B: nasze konto aplikacji, konfigurowane przez <c>Default*</c> w <c>*Options</c>).
    /// </para>
    /// </summary>
    public CarrierAuthInfo? AuthInfo { get; init; }

    /// <summary>Dane adresowe nadawcy.</summary>
    public required Address SenderAddress { get; init; }

    /// <summary>Dane kontaktowe nadawcy.</summary>
    public required ContactInfo SenderContact { get; init; }

    /// <summary>Dane adresowe odbiorcy.</summary>
    public required Address RecipientAddress { get; init; }

    /// <summary>Dane kontaktowe odbiorcy.</summary>
    public required ContactInfo RecipientContact { get; init; }

    /// <summary>
    /// Lista paczek wchodzących w skład przesyłki (minimum jedna).
    /// Większość kurierów obsługuje przesyłki wielopaczkowe.
    /// </summary>
    public required IReadOnlyList<ParcelDimensions> Parcels { get; init; }

    /// <summary>
    /// Kod usługi kuriera (opcjonalny — gdy null, kurier użyje usługi domyślnej).
    /// Np. dla InPost: <c>inpost_courier_standard</c>; dla DHL: <c>AH</c>.
    /// </summary>
    public string? ServiceCode { get; init; }

    /// <summary>Konfiguracja pobrania (COD). Null = brak pobrania.</summary>
    public CodInfo? Cod { get; init; }

    /// <summary>Konfiguracja ubezpieczenia. Null = brak ubezpieczenia.</summary>
    public InsuranceInfo? Insurance { get; init; }

    /// <summary>
    /// Referencja własna nadawcy (numer zamówienia, numer faktury itp.).
    /// Przechowywana przez kuriera i widoczna na etykiecie.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>Dodatkowe uwagi do przesyłki (np. „Ostrożnie — szkło").</summary>
    public string? Comment { get; init; }

    /// <summary>
    /// Kod paczkomatu docelowego (wymagany dla <see cref="Enums.CarrierType.InPostLocker"/>).
    /// <para>
    /// Identyfikator paczkomatu widoczny na mapie InPost, np. <c>WAW123M</c>.
    /// Ignorowany przez adaptery kurierów door-to-door.
    /// </para>
    /// </summary>
    public string? LockerTargetMachineId { get; init; }

    /// <summary>
    /// Czy pobrać etykietę od razu przy rejestracji.
    /// Domyślnie: <c>true</c>. Jeśli <c>false</c>, etykieta w <see cref="ShipmentResult.Label"/> będzie null.
    /// </summary>
    public bool FetchLabel { get; init; } = true;
}
