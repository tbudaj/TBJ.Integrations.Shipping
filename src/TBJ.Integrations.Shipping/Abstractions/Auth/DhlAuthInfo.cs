namespace TBJ.Integrations.Shipping.Abstractions.Auth;

/// <summary>
/// Dane uwierzytelniające do DHL24 WebAPI2, specyficzne dla tenanta.
/// Pobierz z bazy danych i przekaż w każdym żądaniu.
/// </summary>
public sealed class DhlAuthInfo : CarrierAuthInfo
{
    /// <summary>
    /// Login (nazwa użytkownika) konta DHL24.
    /// </summary>
    public required string Login { get; init; }

    /// <summary>
    /// Hasło konta DHL24.
    /// </summary>
    public required string Password { get; init; }
}
