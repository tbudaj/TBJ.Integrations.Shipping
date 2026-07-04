namespace TBJ.Integrations.Shipping.Carriers.UPS.Configuration;

/// <summary>
/// Opcje infrastrukturalne adaptera kurierskiego UPS — wspólne dla wszystkich tenantów.
/// Token OAuth2 jest cache'owany per ClientId w <see cref="UpsTokenCache"/>.
/// <para>
/// <b>Scenariusz A (wielotenantowy):</b> dane uwierzytelniające (ClientId, ClientSecret, AccountNumber)
/// przekazywane per-żądanie przez <c>UpsAuthInfo</c> w <see cref="CreateShipmentRequest"/>
/// i <see cref="OrderPickupRequest"/>. Każdy tenant używa własnych credentials.
/// </para>
/// <para>
/// <b>Scenariusz B (single-tenant / własne konto):</b> dane uwierzytelniające przechowywane
/// w konfiguracji jako <see cref="DefaultClientId"/>, <see cref="DefaultClientSecret"/>
/// i <see cref="DefaultAccountNumber"/>. Używane gdy żądanie nie zawiera <c>UpsAuthInfo</c>.
/// </para>
/// </summary>
public sealed class UpsOptions
{
    /// <summary>
    /// Nazwa sekcji konfiguracyjnej w <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "Shipping:UPS";

    /// <summary>
    /// Bazowy adres URL produkcyjnego UPS API.
    /// Domyślnie: produkcyjny endpoint UPS.
    /// Dla środowiska testowego (CIE): <c>https://wwwcie.ups.com/api</c>.
    /// </summary>
    public string BaseUrl { get; set; } = "https://onlinetools.ups.com/api";

    /// <summary>
    /// Adres URL endpointu do pobierania tokenów OAuth 2.0.
    /// Dla środowiska testowego (CIE): <c>https://wwwcie.ups.com/security/v1/oauth/token</c>.
    /// </summary>
    public string TokenUrl { get; set; } = "https://onlinetools.ups.com/security/v1/oauth/token";

    /// <summary>
    /// Timeout dla żądań HTTP do UPS API.
    /// Domyślnie: 30 sekund.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Domyślny kod usługi UPS używany gdy nie podano w żądaniu.
    /// Domyślnie: 03 (UPS Ground).
    /// </summary>
    public string DefaultServiceCode { get; set; } = "03";

    // ── Scenariusz B: domyślne credentials naszego konta (opcjonalne) ──────────────

    /// <summary>
    /// Domyślny Client ID naszego konta UPS OAuth 2.0 (Scenariusz B).
    /// Używany gdy żądanie nie zawiera <c>UpsAuthInfo</c>.
    /// Uzyskaj w panelu: https://developer.ups.com → My Apps.
    /// </summary>
    public string? DefaultClientId { get; set; }

    /// <summary>
    /// Domyślny Client Secret naszego konta UPS OAuth 2.0 (Scenariusz B).
    /// Używany gdy żądanie nie zawiera <c>UpsAuthInfo</c>.
    /// </summary>
    public string? DefaultClientSecret { get; set; }

    /// <summary>
    /// Domyślny numer konta UPS (Shipper Number) naszej firmy (Scenariusz B).
    /// Używany gdy żądanie nie zawiera <c>UpsAuthInfo</c>.
    /// </summary>
    public string? DefaultAccountNumber { get; set; }
}
