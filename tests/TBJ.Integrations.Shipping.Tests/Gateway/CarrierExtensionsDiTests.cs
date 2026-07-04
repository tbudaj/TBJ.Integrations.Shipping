using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Carriers.FedEx;
using TBJ.Integrations.Shipping.Carriers.GLS;
using TBJ.Integrations.Shipping.Gateway;
using TBJ.Integrations.Shipping.Carriers.UPS;
using Microsoft.Extensions.DependencyInjection;

namespace TBJ.Integrations.Shipping.Tests.Gateway;

/// <summary>
/// Testy integracyjne rejestracji DI dla adapterów GLS, UPS i FedEx
/// za pomocą metod rozszerzających <see cref="ShippingGatewayBuilder"/>.
/// Dane uwierzytelniające (ClientId, ClientSecret, AccountNumber, Username, Password itp.)
/// są przekazywane per-żądanie przez <c>*AuthInfo</c> — nie są już elementem konfiguracji DI.
/// </summary>
public class CarrierExtensionsDiTests
{
    // --- GLS ---

    [Fact]
    public void AddGLS_PoprawnaKonfiguracja_RejestrujeIShippingClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act — dane infrastrukturalne wystarczą (auth przechodzi per-żądanie)
        var builder = services.AddShippingGateway();
        builder.AddGLS(opt =>
        {
            opt.BaseUrl = "https://adeplus.gls-group.eu/adeplus/pod2_adeplus.php";
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var clients = provider.CreateScope().ServiceProvider.GetServices<IShippingClient>();
        Assert.Contains(clients, c => c.Carrier == Abstractions.Enums.CarrierType.GLS);
    }

    [Fact]
    public void AddGLS_BrakBaseUrl_RzucaInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddShippingGateway();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            builder.AddGLS(opt => opt.BaseUrl = string.Empty));
    }

    [Fact]
    public void AddGLS_NieprawidlowyUrl_RzucaInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddShippingGateway();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            builder.AddGLS(opt => opt.BaseUrl = "not-a-url"));
    }

    // --- UPS ---

    [Fact]
    public void AddUPS_PoprawnaKonfiguracja_RejestrujeIShippingClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act — dane infrastrukturalne wystarczą
        var builder = services.AddShippingGateway();
        builder.AddUPS(opt =>
        {
            opt.BaseUrl = "https://onlinetools.ups.com";
            opt.TokenUrl = "https://onlinetools.ups.com/security/v1/oauth/token";
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var clients = provider.CreateScope().ServiceProvider.GetServices<IShippingClient>();
        Assert.Contains(clients, c => c.Carrier == Abstractions.Enums.CarrierType.UPS);
    }

    [Fact]
    public void AddUPS_BrakBaseUrl_RzucaInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddShippingGateway();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            builder.AddUPS(opt =>
            {
                opt.BaseUrl = string.Empty;
                opt.TokenUrl = "https://onlinetools.ups.com/security/v1/oauth/token";
            }));
    }

    [Fact]
    public void AddUPS_BrakTokenUrl_RzucaInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddShippingGateway();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            builder.AddUPS(opt =>
            {
                opt.BaseUrl = "https://onlinetools.ups.com";
                opt.TokenUrl = "not-a-url";
            }));
    }

    // --- FedEx ---

    [Fact]
    public void AddFedEx_PoprawnaKonfiguracja_RejestrujeIShippingClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act — dane infrastrukturalne wystarczą
        var builder = services.AddShippingGateway();
        builder.AddFedEx(opt =>
        {
            opt.BaseUrl = "https://apis.fedex.com";
            opt.TokenUrl = "https://apis.fedex.com/oauth/token";
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var clients = provider.CreateScope().ServiceProvider.GetServices<IShippingClient>();
        Assert.Contains(clients, c => c.Carrier == Abstractions.Enums.CarrierType.FedEx);
    }

    [Fact]
    public void AddFedEx_BrakBaseUrl_RzucaInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddShippingGateway();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            builder.AddFedEx(opt =>
            {
                opt.BaseUrl = string.Empty;
                opt.TokenUrl = "https://apis.fedex.com/oauth/token";
            }));
    }

    [Fact]
    public void AddFedEx_BrakTokenUrl_RzucaInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddShippingGateway();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            builder.AddFedEx(opt =>
            {
                opt.BaseUrl = "https://apis.fedex.com";
                opt.TokenUrl = "not-a-url";
            }));
    }

    // --- Wielokrotne adaptery ---

    [Fact]
    public void AddGLS_AddUPS_AddFedEx_RejestrujeWszystkichAdapterów()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddShippingGateway()
            .AddGLS(opt => opt.BaseUrl = "https://adeplus.gls-group.eu/adeplus/pod2_adeplus.php")
            .AddUPS(opt =>
            {
                opt.BaseUrl = "https://onlinetools.ups.com";
                opt.TokenUrl = "https://onlinetools.ups.com/security/v1/oauth/token";
            })
            .AddFedEx(opt =>
            {
                opt.BaseUrl = "https://apis.fedex.com";
                opt.TokenUrl = "https://apis.fedex.com/oauth/token";
            });
        var provider = services.BuildServiceProvider();

        // Assert
        var clients = provider.CreateScope().ServiceProvider.GetServices<IShippingClient>().ToList();
        Assert.Contains(clients, c => c.Carrier == Abstractions.Enums.CarrierType.GLS);
        Assert.Contains(clients, c => c.Carrier == Abstractions.Enums.CarrierType.UPS);
        Assert.Contains(clients, c => c.Carrier == Abstractions.Enums.CarrierType.FedEx);
    }
}
