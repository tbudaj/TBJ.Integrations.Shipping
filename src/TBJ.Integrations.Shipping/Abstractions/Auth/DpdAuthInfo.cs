namespace TBJ.Integrations.Shipping.Abstractions.Auth;

/// <summary>
/// Dane uwierzytelniające do DPD Polska Web Service, specyficzne dla tenanta.
/// Pobierz z bazy danych i przekaż w każdym żądaniu.
/// </summary>
public sealed class DpdAuthInfo : CarrierAuthInfo
{
    /// <summary>
    /// Nazwa użytkownika (login/e-mail) konta DPD.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Hasło konta DPD.
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// Facility ID — numer punktu odbioru DPD nadawcy.
    /// Widoczny w portalu dpd.com.pl → Moje konto → Informacje o koncie.
    /// </summary>
    public required int Fid { get; init; }
}
