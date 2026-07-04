namespace TBJ.Integrations.Shipping.Abstractions.Auth;

/// <summary>
/// Dane uwierzytelniające do FedEx REST API (OAuth 2.0), specyficzne dla tenanta.
/// Pobierz z bazy danych i przekaż w każdym żądaniu.
/// Token OAuth2 jest pobierany i cache'owany automatycznie per ClientId.
/// </summary>
public sealed class FedExAuthInfo : CarrierAuthInfo
{
    /// <summary>
    /// API Key (Client ID) aplikacji OAuth 2.0 z panelu developer.fedex.com.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Secret Key (Client Secret) aplikacji OAuth 2.0 z panelu developer.fedex.com.
    /// </summary>
    public required string ClientSecret { get; init; }

    /// <summary>
    /// Numer konta FedEx (9 cyfr) do rozliczania przesyłek.
    /// </summary>
    public required string AccountNumber { get; init; }
}
