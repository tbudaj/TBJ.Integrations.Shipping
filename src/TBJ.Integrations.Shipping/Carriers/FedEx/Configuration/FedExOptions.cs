namespace TBJ.Integrations.Shipping.Carriers.FedEx.Configuration;

/// <summary>
/// Opcje adaptera kurierskiego FedEx — infrastrukturalne oraz opcjonalne domyślne credentials.
/// <para>
/// Dane uwierzytelniające mogą być dostarczane na dwa sposoby:
/// <list type="bullet">
/// <item><description>
/// <b>Per-żądanie (Scenariusz A, konto tenanta):</b> przez <c>FedExAuthInfo</c> w żądaniu.
/// Token OAuth2 jest cache'owany per ClientId w <c>FedExTokenCache</c>.
/// </description></item>
/// <item><description>
/// <b>Z konfiguracji (Scenariusz B, nasze konto):</b> przez pola <c>Default*</c> poniżej —
/// używane automatycznie gdy żądanie nie zawiera <c>FedExAuthInfo</c>.
/// </description></item>
/// </list>
/// </para>
/// </summary>
public sealed class FedExOptions
{
    /// <summary>
    /// Nazwa sekcji konfiguracyjnej w <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "Shipping:FedEx";

    /// <summary>
    /// Bazowy adres URL produkcyjnego FedEx API.
    /// Domyślnie: produkcyjny endpoint FedEx.
    /// Dla środowiska testowego (Sandbox): <c>https://apis-sandbox.fedex.com</c>.
    /// </summary>
    public string BaseUrl { get; set; } = "https://apis.fedex.com";

    /// <summary>
    /// Adres URL endpointu do pobierania tokenów OAuth 2.0.
    /// Dla środowiska testowego (Sandbox): <c>https://apis-sandbox.fedex.com/oauth/token</c>.
    /// </summary>
    public string TokenUrl { get; set; } = "https://apis.fedex.com/oauth/token";

    /// <summary>
    /// Timeout dla żądań HTTP do FedEx API.
    /// Domyślnie: 30 sekund.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Domyślny typ usługi FedEx używany gdy nie podano w żądaniu.
    /// Domyślnie: INTERNATIONAL_ECONOMY.
    /// </summary>
    public string DefaultServiceType { get; set; } = "INTERNATIONAL_ECONOMY";

    // ── Scenariusz B: domyślne credentials naszego konta (opcjonalne) ──────────────

    /// <summary>
    /// Domyślny Client ID naszego konta FedEx OAuth 2.0 (Scenariusz B).
    /// Używany gdy żądanie nie zawiera <c>FedExAuthInfo</c>.
    /// Uzyskaj w panelu: https://developer.fedex.com → My Projects.
    /// </summary>
    public string? DefaultClientId { get; set; }

    /// <summary>
    /// Domyślny Client Secret naszego konta FedEx OAuth 2.0 (Scenariusz B).
    /// Używany gdy żądanie nie zawiera <c>FedExAuthInfo</c>.
    /// </summary>
    public string? DefaultClientSecret { get; set; }

    /// <summary>
    /// Domyślny numer konta FedEx naszej firmy (Scenariusz B).
    /// Używany gdy żądanie nie zawiera <c>FedExAuthInfo</c>.
    /// </summary>
    public string? DefaultAccountNumber { get; set; }
}
