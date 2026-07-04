namespace TBJ.Integrations.Shipping.Abstractions.Auth;

/// <summary>
/// Dane uwierzytelniające do GLS ADE-Plus WebAPI, specyficzne dla tenanta.
/// Pobierz z bazy danych i przekaż w każdym żądaniu.
/// </summary>
public sealed class GlsAuthInfo : CarrierAuthInfo
{
    /// <summary>
    /// Nazwa użytkownika konta GLS ADE-Plus.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Hasło konta GLS ADE-Plus.
    /// </summary>
    public required string Password { get; init; }
}
