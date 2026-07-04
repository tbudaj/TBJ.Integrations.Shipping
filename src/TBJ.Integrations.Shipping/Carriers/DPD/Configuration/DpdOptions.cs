namespace TBJ.Integrations.Shipping.Carriers.DPD.Configuration;

/// <summary>
/// Opcje adaptera kurierskiego DPD Polska — infrastrukturalne oraz opcjonalne domyślne credentials.
/// <para>
/// Dane uwierzytelniające mogą być dostarczane na dwa sposoby:
/// <list type="bullet">
/// <item><description>
/// <b>Per-żądanie (Scenariusz A, konto tenanta):</b> przez <c>DpdAuthInfo</c> w żądaniu.
/// </description></item>
/// <item><description>
/// <b>Z konfiguracji (Scenariusz B, nasze konto):</b> przez pola <c>Default*</c> poniżej —
/// używane automatycznie gdy żądanie nie zawiera <c>DpdAuthInfo</c>.
/// </description></item>
/// </list>
/// </para>
/// </summary>
public sealed class DpdOptions
{
    /// <summary>
    /// Nazwa sekcji konfiguracyjnej w <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "Shipping:DPD";

    /// <summary>
    /// Bazowy adres URL usługi DPD Web Service.
    /// Domyślnie: produkcyjny endpoint DPD Polska.
    /// </summary>
    public string BaseUrl { get; set; } = "https://dpdservices.dpd.com.pl/DPDPackageObjServicesService/DPDPackageObjServices";

    /// <summary>
    /// Timeout dla żądań SOAP do DPD.
    /// Domyślnie: 60 sekund.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Format generowanej etykiety (PDF lub ZPL).
    /// Domyślnie: PDF.
    /// </summary>
    public string LabelFormat { get; set; } = "PDF";

    /// <summary>
    /// Format strony etykiety (np. A4, A5).
    /// Domyślnie: A4.
    /// </summary>
    public string LabelPageFormat { get; set; } = "A4";

    // ── Scenariusz B: domyślne credentials naszego konta (opcjonalne) ──────────────

    /// <summary>
    /// Domyślna nazwa użytkownika naszego konta DPD (Scenariusz B).
    /// Używana gdy żądanie nie zawiera <c>DpdAuthInfo</c>.
    /// </summary>
    public string? DefaultUsername { get; set; }

    /// <summary>
    /// Domyślne hasło naszego konta DPD (Scenariusz B).
    /// Używane gdy żądanie nie zawiera <c>DpdAuthInfo</c>.
    /// </summary>
    public string? DefaultPassword { get; set; }

    /// <summary>
    /// Domyślny Facility ID naszego konta DPD (Scenariusz B).
    /// Używany gdy żądanie nie zawiera <c>DpdAuthInfo</c>.
    /// Widoczny w portalu dpd.com.pl → Moje konto → Informacje o koncie.
    /// </summary>
    public int? DefaultFid { get; set; }
}
