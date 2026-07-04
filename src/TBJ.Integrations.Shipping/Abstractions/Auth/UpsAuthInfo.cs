namespace TBJ.Integrations.Shipping.Abstractions.Auth;

/// <summary>
/// Dane uwierzytelniające do UPS REST API (OAuth 2.0), specyficzne dla tenanta.
/// Pobierz z bazy danych i przekaż w każdym żądaniu.
/// Token OAuth2 jest pobierany i cache'owany automatycznie per ClientId.
/// </summary>
public sealed class UpsAuthInfo : CarrierAuthInfo
{
    /// <summary>
    /// Client ID aplikacji OAuth 2.0 z panelu developer.ups.com.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Client Secret aplikacji OAuth 2.0 z panelu developer.ups.com.
    /// </summary>
    public required string ClientSecret { get; init; }

    /// <summary>
    /// Numer konta UPS (Shipper Number) do rozliczania przesyłek.
    /// </summary>
    public required string AccountNumber { get; init; }
}
