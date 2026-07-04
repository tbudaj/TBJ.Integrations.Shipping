namespace TBJ.Integrations.Shipping.Carriers.DHL.Configuration;

/// <summary>
/// Opcje infrastrukturalne adaptera kurierskiego DHL24 Poland — wspólne dla wszystkich tenantów.
/// <para>
/// <b>Scenariusz A (wielotenantowy):</b> dane uwierzytelniające (Login, Password) przekazywane
/// per-żądanie przez <c>DhlAuthInfo</c> w <see cref="CreateShipmentRequest"/>
/// i <see cref="OrderPickupRequest"/>. Każdy tenant używa własnych credentials.
/// </para>
/// <para>
/// <b>Scenariusz B (single-tenant / własne konto):</b> dane uwierzytelniające przechowywane
/// w konfiguracji jako <see cref="DefaultLogin"/> i <see cref="DefaultPassword"/>.
/// Używane gdy żądanie nie zawiera <c>DhlAuthInfo</c>.
/// </para>
/// </summary>
public sealed class DhlOptions
{
    /// <summary>
    /// Nazwa sekcji konfiguracyjnej w <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "Shipping:DHL";

    /// <summary>
    /// Bazowy adres URL usługi DHL24 WebAPI2.
    /// Domyślnie: produkcyjny endpoint DHL Polska.
    /// </summary>
    public string BaseUrl { get; set; } = "https://dhl24.com.pl/webapi2/provider/service.html?ws=1";

    /// <summary>
    /// Timeout dla żądań SOAP do DHL.
    /// Domyślnie: 60 sekund.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Domyślny kod usługi DHL (AH = krajowa, SP = międzynarodowa, 09 = 9:00, 12 = 12:00).
    /// Domyślnie: AH.
    /// </summary>
    public string DefaultServiceType { get; set; } = "AH";

    // ── Scenariusz B: domyślne credentials naszego konta (opcjonalne) ──────────────

    /// <summary>
    /// Domyślny login naszego konta DHL24 (Scenariusz B).
    /// Używany gdy żądanie nie zawiera <c>DhlAuthInfo</c>.
    /// </summary>
    public string? DefaultLogin { get; set; }

    /// <summary>
    /// Domyślne hasło naszego konta DHL24 (Scenariusz B).
    /// Używane gdy żądanie nie zawiera <c>DhlAuthInfo</c>.
    /// </summary>
    public string? DefaultPassword { get; set; }
}
