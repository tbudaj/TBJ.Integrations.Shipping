namespace TBJ.Integrations.Shipping.Abstractions.Auth;

/// <summary>
/// Dane uwierzytelniające do InPost ShipX API, specyficzne dla tenanta.
/// Pobierz z bazy danych i przekaż w każdym żądaniu.
/// </summary>
public sealed class InPostAuthInfo : CarrierAuthInfo
{
    /// <summary>
    /// Token dostępu (Bearer) do InPost ShipX API.
    /// Uzyskaj w panelu: https://manager.paczkomaty.pl → Ustawienia → API → Klucze dostępowe.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Identyfikator organizacji InPost (org_id), używany w ścieżkach endpointów.
    /// Widoczny w panelu: Ustawienia → Informacje o firmie → ID organizacji.
    /// </summary>
    public required string OrganizationId { get; init; }
}
