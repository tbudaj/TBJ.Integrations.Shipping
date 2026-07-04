using TBJ.Integrations.Shipping.Gateway;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace TBJ.Integrations.Shipping;

/// <summary>
/// Rozszerzenia rejestracji bramy wysyłkowej w kontenerze DI.
/// </summary>
public static class ShippingGatewayExtensions
{
    /// <summary>
    /// Rejestruje główną bramę wysyłkową (<see cref="IShippingGateway"/>) i zwraca budowniczego
    /// umożliwiającego rejestrację adapterów konkretnych kurierów.
    /// </summary>
    /// <param name="services">Kolekcja serwisów DI.</param>
    /// <returns>Budowniczy bramy wysyłkowej (fluent API).</returns>
    public static ShippingGatewayBuilder AddShippingGateway(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IShippingGateway, ShippingGateway>();
        return new ShippingGatewayBuilder(services);
    }
}
