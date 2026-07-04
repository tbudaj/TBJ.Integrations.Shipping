namespace TBJ.Integrations.Shipping.Carriers.GLS.Configuration;

/// <summary>
/// Opcje infrastrukturalne adaptera kurierskiego GLS Polska — wspólne dla wszystkich tenantów.
/// <para>
/// <b>Scenariusz A (wielotenantowy):</b> dane uwierzytelniające (Username, Password) przekazywane
/// per-żądanie przez <c>GlsAuthInfo</c> w <see cref="CreateShipmentRequest"/>
/// i <see cref="OrderPickupRequest"/>. Każdy tenant używa własnych credentials.
/// </para>
/// <para>
/// <b>Scenariusz B (single-tenant / własne konto):</b> dane uwierzytelniające przechowywane
/// w konfiguracji jako <see cref="DefaultUsername"/> i <see cref="DefaultPassword"/>.
/// Używane gdy żądanie nie zawiera <c>GlsAuthInfo</c>.
/// </para>
/// </summary>
public sealed class GlsOptions
{
    /// <summary>
    /// Nazwa sekcji konfiguracyjnej w <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "Shipping:GLS";

    /// <summary>
    /// Bazowy adres URL usługi GLS ADE-Plus WebAPI.
    /// Domyślnie: produkcyjny endpoint GLS Polska.
    /// </summary>
    public string BaseUrl { get; set; } = "https://adeplus.gls-poland.com/adeplus/pm1/ade_webapi.php";

    /// <summary>
    /// Timeout dla żądań SOAP do GLS.
    /// Domyślnie: 60 sekund.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Kod kraju nadawcy używany przy rejestracji przesyłek.
    /// Domyślnie: PL.
    /// </summary>
    public string CountryCode { get; set; } = "PL";

    // ── Scenariusz B: domyślne credentials naszego konta (opcjonalne) ──────────────

    /// <summary>
    /// Domyślna nazwa użytkownika naszego konta GLS ADE-Plus (Scenariusz B).
    /// Używana gdy żądanie nie zawiera <c>GlsAuthInfo</c>.
    /// </summary>
    public string? DefaultUsername { get; set; }

    /// <summary>
    /// Domyślne hasło naszego konta GLS ADE-Plus (Scenariusz B).
    /// Używane gdy żądanie nie zawiera <c>GlsAuthInfo</c>.
    /// </summary>
    public string? DefaultPassword { get; set; }
}
