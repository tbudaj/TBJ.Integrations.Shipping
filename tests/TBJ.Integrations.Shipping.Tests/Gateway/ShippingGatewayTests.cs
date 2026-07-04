using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TBJ.Integrations.Shipping.Tests.Gateway;

/// <summary>
/// Testy jednostkowe dla <see cref="ShippingGateway"/> — weryfikują routing do adapterów
/// kurierskich na podstawie parametru <see cref="CarrierType"/>.
/// </summary>
public class ShippingGatewayTests
{
    private readonly Mock<IShippingClient> _inpostClientMock;
    private readonly Mock<IShippingClient> _dpdClientMock;
    private readonly IShippingGateway _gateway;

    public ShippingGatewayTests()
    {
        _inpostClientMock = new Mock<IShippingClient>();
        _inpostClientMock.Setup(c => c.Carrier).Returns(CarrierType.InPostLocker);

        _dpdClientMock = new Mock<IShippingClient>();
        _dpdClientMock.Setup(c => c.Carrier).Returns(CarrierType.DPD);

        var clients = new List<IShippingClient>
        {
            _inpostClientMock.Object,
            _dpdClientMock.Object,
        };

        _gateway = new ShippingGateway(clients, NullLogger<ShippingGateway>.Instance);
    }

    // --- CreateShipmentAsync ---

    [Fact]
    public async Task CreateShipmentAsync_DlaInPostLocker_PowinienWywolaćAdapterInPostLocker()
    {
        // Arrange
        var request = BuildCreateShipmentRequest();
        var expected = BuildShipmentResult("InPost-123");
        _inpostClientMock
            .Setup(c => c.CreateShipmentAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _gateway.CreateShipmentAsync(CarrierType.InPostLocker, request);

        // Assert
        Assert.Equal(expected.TrackingNumber, result.TrackingNumber);
        _inpostClientMock.Verify(c => c.CreateShipmentAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _dpdClientMock.Verify(c => c.CreateShipmentAsync(It.IsAny<CreateShipmentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateShipmentAsync_DlaDPD_PowinienWywolaćAdapterDPD()
    {
        // Arrange
        var request = BuildCreateShipmentRequest();
        var expected = BuildShipmentResult("DPD-456");
        _dpdClientMock
            .Setup(c => c.CreateShipmentAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _gateway.CreateShipmentAsync(CarrierType.DPD, request);

        // Assert
        Assert.Equal(expected.TrackingNumber, result.TrackingNumber);
        _inpostClientMock.Verify(c => c.CreateShipmentAsync(It.IsAny<CreateShipmentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateShipmentAsync_BrakAdaptera_PowinienRzucićInvalidOperationException()
    {
        // Arrange
        var request = BuildCreateShipmentRequest();

        // Act & Assert — UPS nie jest zarejestrowany w tym gateway
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _gateway.CreateShipmentAsync(CarrierType.UPS, request));
    }

    // --- TrackShipmentAsync ---

    [Fact]
    public async Task TrackShipmentAsync_DlaInPostLocker_PowinienWywolaćAdapterInPostLocker()
    {
        // Arrange
        const string trackingNumber = "123456789012";
        var authInfo = new InPostAuthInfo { AccessToken = "tok", OrganizationId = "org" };
        var expected = BuildTrackingResult(trackingNumber, ShipmentStatus.InTransit);
        _inpostClientMock
            .Setup(c => c.TrackShipmentAsync(authInfo, trackingNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _gateway.TrackShipmentAsync(CarrierType.InPostLocker, authInfo, trackingNumber);

        // Assert
        Assert.Equal(ShipmentStatus.InTransit, result.CurrentStatus);
        Assert.Equal(trackingNumber, result.TrackingNumber);
    }

    [Fact]
    public async Task TrackShipmentAsync_BrakAdaptera_PowinienRzucićInvalidOperationException()
    {
        // Arrange
        var authInfo = new GlsAuthInfo { Username = "u", Password = "p" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _gateway.TrackShipmentAsync(CarrierType.GLS, authInfo, "123456"));
    }

    // --- OrderPickupAsync ---

    [Fact]
    public async Task OrderPickupAsync_DlaInPostLocker_PowinienWywolaćAdapterInPostLocker()
    {
        // Arrange
        var request = BuildOrderPickupRequest("InPost-123");
        var expected = new PickupResult { PickupOrderId = "PO-001" };
        _inpostClientMock
            .Setup(c => c.OrderPickupAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _gateway.OrderPickupAsync(CarrierType.InPostLocker, request);

        // Assert
        Assert.Equal("PO-001", result.PickupOrderId);
    }

    // --- GetDeliveryConfirmationAsync ---

    [Fact]
    public async Task GetDeliveryConfirmationAsync_DlaInPostLocker_PowinienZwrócićPOD()
    {
        // Arrange
        const string trackingNumber = "123456789012";
        var authInfo = new InPostAuthInfo { AccessToken = "tok", OrganizationId = "org" };
        var deliveredAt = DateTimeOffset.UtcNow.AddHours(-2);
        var expected = new DeliveryConfirmationResult
        {
            TrackingNumber = trackingNumber,
            DeliveredAt = deliveredAt,
            ReceivedBy = "Jan Kowalski",
        };
        _inpostClientMock
            .Setup(c => c.GetDeliveryConfirmationAsync(authInfo, trackingNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _gateway.GetDeliveryConfirmationAsync(CarrierType.InPostLocker, authInfo, trackingNumber);

        // Assert
        Assert.Equal("Jan Kowalski", result.ReceivedBy);
        Assert.Equal(deliveredAt, result.DeliveredAt);
    }

    // --- GetClient ---

    [Fact]
    public void GetClient_DlaZarejestrowanegoCouriera_ZwracaAdapter()
    {
        // Act
        var client = _gateway.GetClient(CarrierType.InPostLocker);

        // Assert
        Assert.Equal(CarrierType.InPostLocker, client.Carrier);
    }

    [Fact]
    public void GetClient_DlaNiezarejestrowanegoCouriera_RzucaInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _gateway.GetClient(CarrierType.FedEx));
        Assert.Contains("FedEx", ex.Message);
    }

    // --- Helpers ---

    private static CreateShipmentRequest BuildCreateShipmentRequest() => new()
    {
        AuthInfo = new InPostAuthInfo { AccessToken = "test-token", OrganizationId = "test-org" },
        SenderAddress = new Address
        {
            Name = "Firma Testowa",
            Street = "ul. Testowa 1",
            PostalCode = "00-001",
            City = "Warszawa",
        },
        SenderContact = new ContactInfo { Name = "Adam Nowak", Phone = "+48123456789" },
        RecipientAddress = new Address
        {
            Name = "Odbiorca Testowy",
            Street = "ul. Odbiorcza 2",
            PostalCode = "30-001",
            City = "Kraków",
        },
        RecipientContact = new ContactInfo { Name = "Maria Kowalska", Phone = "+48987654321" },
        Parcels = new List<ParcelDimensions>
        {
            new() { WeightKg = 2.5m, LengthCm = 30, WidthCm = 20, HeightCm = 15 },
        },
    };

    private static ShipmentResult BuildShipmentResult(string trackingNumber) => new()
    {
        TrackingNumber = trackingNumber,
        CarrierShipmentId = trackingNumber + "-ID",
    };

    private static TrackingResult BuildTrackingResult(string trackingNumber, ShipmentStatus status) => new()
    {
        TrackingNumber = trackingNumber,
        CurrentStatus = status,
        StatusDescription = status.ToString(),
        Events = new List<TrackingEvent>
        {
            new()
            {
                OccurredAt = DateTimeOffset.UtcNow,
                Status = status,
                Description = "Zdarzenie testowe",
            },
        },
    };

    private static OrderPickupRequest BuildOrderPickupRequest(string shipmentId) => new()
    {
        AuthInfo = new InPostAuthInfo { AccessToken = "test-token", OrganizationId = "test-org" },
        ShipmentIds = new List<string> { shipmentId },
        PickupAddress = new Address
        {
            Name = "Firma Testowa",
            Street = "ul. Testowa 1",
            PostalCode = "00-001",
            City = "Warszawa",
        },
        PickupContact = new ContactInfo { Name = "Adam Nowak", Phone = "+48123456789" },
        PickupDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
    };
}
