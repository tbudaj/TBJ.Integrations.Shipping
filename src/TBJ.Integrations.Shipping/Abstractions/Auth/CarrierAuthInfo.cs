namespace TBJ.Integrations.Shipping.Abstractions.Auth;

/// <summary>
/// Bazowa klasa danych uwierzytelniających do API kuriera.
/// Dane te są specyficzne dla tenanta (realizatora) i powinny być przechowywane
/// w bazie danych aplikacji nadrzędnej — nie w <c>appsettings.json</c>.
/// </summary>
public abstract class CarrierAuthInfo;
