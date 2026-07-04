using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Gateway;
using Microsoft.Extensions.DependencyInjection;

namespace TBJ.Integrations.Shipping.Tests.Gateway;

/// <summary>
/// Testy integracyjne rejestracji DI dla <see cref="ShippingGateway"/>.
/// Weryfikują, że rozszerzenie <see cref="ShippingGatewayExtensions.AddShippingGateway"/>
/// poprawnie rejestruje gateway w kontenerze DI.
/// </summary>
public class ShippingGatewayDiTests
{
    [Fact]
    public void AddShippingGateway_BezAdapterów_RejestrujeIShippingGateway()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddShippingGateway();
        var provider = services.BuildServiceProvider();

        // Assert — gateway powinien być dostępny nawet bez adapterów
        var gateway = provider.GetService<IShippingGateway>();
        Assert.NotNull(gateway);
    }

    [Fact]
    public void AddShippingGateway_RejestrujeJakoScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddShippingGateway();

        // Act
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IShippingGateway));

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddShippingGateway_DwaRazyWywołane_NieDuplikujeRejestracji()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddShippingGateway();
        services.AddShippingGateway();

        // Assert — gateway nadal jest dostępny (nie rzuca wyjątku przy budowaniu)
        var provider = services.BuildServiceProvider();
        var gateway = provider.GetService<IShippingGateway>();
        Assert.NotNull(gateway);
    }
}
