using Microsoft.Extensions.DependencyInjection;

namespace TBJ.Integrations.Shipping;

/// <summary>
/// Budowniczy konfiguracji bramy wysyłkowej.
/// Umożliwia rejestrowanie adapterów konkretnych kurierów za pomocą metod rozszerzających
/// takich jak <c>AddDPD</c> czy <c>AddDHL</c>.
/// </summary>
public sealed class ShippingGatewayBuilder
{
    /// <summary>
    /// Inicjalizuje nową instancję budowniczego na podstawie kolekcji serwisów.
    /// </summary>
    /// <param name="services">Kolekcja serwisów DI.</param>
    public ShippingGatewayBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>
    /// Kolekcja serwisów DI używana do rejestracji adapterów kurierskich.
    /// </summary>
    public IServiceCollection Services { get; }
}
