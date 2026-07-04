namespace TBJ.Integrations.Shipping.Carriers.InPost.Configuration;

/// <summary>
/// Opcje klienta InPost ShipX API — infrastrukturalne oraz opcjonalne domyślne credentials.
/// <para>
/// Dane uwierzytelniające mogą być dostarczane na dwa sposoby:
/// <list type="bullet">
/// <item><description>
/// <b>Per-żądanie (Scenariusz A, konto tenanta):</b> przez <c>InPostAuthInfo</c> w żądaniu.
/// </description></item>
/// <item><description>
/// <b>Z konfiguracji (Scenariusz B, nasze konto):</b> przez pola <c>Default*</c> poniżej —
/// używane automatycznie gdy żądanie nie zawiera <c>InPostAuthInfo</c>.
/// </description></item>
/// </list>
/// </para>
/// </summary>
public sealed class InPostOptions
{
    /// <summary>
    /// Nazwa sekcji konfiguracyjnej w <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "Shipping:InPost";

    /// <summary>
    /// Bazowy adres URL InPost ShipX API.
    /// Domyślnie: <c>https://api-shipx-pl.easypack24.net</c> (środowisko produkcyjne).
    /// Dla środowiska testowego: <c>https://sandbox-api-shipx-pl.easypack24.net</c>.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api-shipx-pl.easypack24.net";

    /// <summary>
    /// Timeout dla żądań HTTP do InPost ShipX API.
    /// Domyślnie: 30 sekund.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Format etykiety wysyłkowej.
    /// Domyślnie: <c>pdf</c>. Obsługiwane wartości: <c>pdf</c>, <c>zpl</c>, <c>epl</c>.
    /// </summary>
    public string LabelFormat { get; set; } = "pdf";

    // ── Scenariusz B: domyślne credentials naszego konta (opcjonalne) ──────────────

    /// <summary>
    /// Domyślny Bearer token naszego konta InPost (Scenariusz B).
    /// Używany gdy żądanie nie zawiera <c>InPostAuthInfo</c>.
    /// Uzyskaj w panelu: https://manager.paczkomaty.pl → Ustawienia → API → Klucze dostępowe.
    /// </summary>
    public string? DefaultAccessToken { get; set; }

    /// <summary>
    /// Domyślny identyfikator organizacji naszego konta InPost (Scenariusz B).
    /// Używany gdy żądanie nie zawiera <c>InPostAuthInfo</c>.
    /// Widoczny w panelu: Ustawienia → Informacje o firmie → ID organizacji.
    /// </summary>
    public string? DefaultOrganizationId { get; set; }
}
