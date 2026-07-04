using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.DHL.Configuration;
using TBJ.Integrations.Shipping.Carriers.DHL.Mappers;

namespace TBJ.Integrations.Shipping.Tests.Mappers;

/// <summary>
/// Testy jednostkowe dla <see cref="DhlMapper"/> — weryfikują poprawność budowania XML
/// i mapowania odpowiedzi DHL24 na modele abstrakcji.
/// </summary>
public class DhlMapperTests
{
    private readonly DhlOptions _options = new() { DefaultServiceType = "AH" };

    // --- BuildShipmentXml ---

    [Fact]
    public void BuildShipmentXml_PoprawneDane_ZawieraWymaganeElementy()
    {
        // Arrange
        var request = BuildRequest();

        // Act
        var xml = DhlMapper.BuildShipmentXml(request, _options);

        // Assert
        Assert.Contains("<ns:shipments>", xml);
        Assert.Contains("<ns:serviceType>AH</ns:serviceType>", xml);
        Assert.Contains("Firma Testowa", xml);
        Assert.Contains("Odbiorca Testowy", xml);
    }

    [Fact]
    public void BuildShipmentXml_ZKodemUsługiWRequest_UżywaKoduZRequest()
    {
        // Arrange
        var request = BuildRequest();
        var requestWithService = new CreateShipmentRequest
        {
            AuthInfo = new DhlAuthInfo { Login = "test_login", Password = "test_pass" },
            SenderAddress = request.SenderAddress,
            SenderContact = request.SenderContact,
            RecipientAddress = request.RecipientAddress,
            RecipientContact = request.RecipientContact,
            Parcels = request.Parcels,
            ServiceCode = "09",
        };

        // Act
        var xml = DhlMapper.BuildShipmentXml(requestWithService, _options);

        // Assert
        Assert.Contains("<ns:serviceType>09</ns:serviceType>", xml);
        Assert.DoesNotContain("<ns:serviceType>AH</ns:serviceType>", xml);
    }

    // --- BuildBookCourierXml ---

    [Fact]
    public void BuildBookCourierXml_ZawieraNumerPrzesyłki()
    {
        // Arrange
        var request = BuildPickupRequest();

        // Act
        var xml = DhlMapper.BuildBookCourierXml(request, new List<string> { "SHIP-001", "SHIP-002" });

        // Assert
        Assert.Contains("<ns:shipmentTime>", xml);
        Assert.Contains("SHIP-001", xml);
        Assert.Contains("SHIP-002", xml);
    }

    // --- ToShipmentResult ---

    [Fact]
    public void ToShipmentResult_ZEtykietą_ZwracaLabelResult()
    {
        // Arrange
        var labelBytes = new byte[] { 10, 20, 30 };

        // Act
        var result = DhlMapper.ToShipmentResult("DHL-123", "LABEL-456", labelBytes);

        // Assert
        Assert.Equal("DHL-123", result.TrackingNumber);
        Assert.Equal("DHL-123", result.CarrierShipmentId);
        Assert.NotNull(result.Label);
        Assert.Equal("application/pdf", result.Label!.ContentType);
    }

    // --- ToTrackingResult (status mapping) ---

    [Theory]
    [InlineData("delivered", ShipmentStatus.Delivered)]
    [InlineData("DELIVERED", ShipmentStatus.Delivered)]
    [InlineData("in transit", ShipmentStatus.InTransit)]
    [InlineData("out for delivery", ShipmentStatus.OutForDelivery)]
    [InlineData("picked up", ShipmentStatus.PickedUp)]
    [InlineData("attempt failed", ShipmentStatus.DeliveryAttemptFailed)]
    [InlineData("returned", ShipmentStatus.ReturnedToSender)]
    [InlineData("nieznany_status", ShipmentStatus.Unknown)]
    public void ToTrackingResult_MapujeStatusyDHLNaAbstrakcję(string dhlStatus, ShipmentStatus expectedStatus)
    {
        // Arrange
        var events = new List<(string status, string desc, DateTimeOffset time, string? loc)>
        {
            (dhlStatus, "Opis zdarzenia", DateTimeOffset.UtcNow, "Warszawa"),
        };

        // Act
        var result = DhlMapper.ToTrackingResult("DHL-123", events);

        // Assert
        Assert.Equal(expectedStatus, result.CurrentStatus);
    }

    [Fact]
    public void ToTrackingResult_PustaListaZdarzeń_ZwracaStatusUnknown()
    {
        // Act
        var result = DhlMapper.ToTrackingResult("DHL-123", new List<(string, string, DateTimeOffset, string?)>());

        // Assert
        Assert.Equal(ShipmentStatus.Unknown, result.CurrentStatus);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void ToTrackingResult_WieleZdarzeń_ZwracaNajnowszeJakoAktualny()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var events = new List<(string status, string desc, DateTimeOffset time, string? loc)>
        {
            ("in transit", "W drodze", now.AddHours(-5), null),
            ("out for delivery", "W doręczeniu", now.AddHours(-1), "Kraków"),
        };

        // Act
        var result = DhlMapper.ToTrackingResult("DHL-123", events);

        // Assert
        Assert.Equal(ShipmentStatus.OutForDelivery, result.CurrentStatus);
        Assert.Equal(2, result.Events.Count);
    }

    // --- Helpers ---

    private static CreateShipmentRequest BuildRequest() => new()
    {
        AuthInfo = new DhlAuthInfo { Login = "test_login", Password = "test_pass" },
        SenderAddress = new Address
        {
            Name = "Firma Testowa",
            Street = "ul. Testowa",
            BuildingNumber = "1",
            PostalCode = "00-001",
            City = "Warszawa",
        },
        SenderContact = new ContactInfo { Name = "Adam Nowak", Phone = "+48123456789" },
        RecipientAddress = new Address
        {
            Name = "Odbiorca Testowy",
            Street = "ul. Odbiorcza",
            BuildingNumber = "2",
            PostalCode = "30-001",
            City = "Kraków",
        },
        RecipientContact = new ContactInfo { Name = "Maria Kowalska", Phone = "+48987654321" },
        Parcels = new List<ParcelDimensions>
        {
            new() { WeightKg = 3.0m, LengthCm = 40, WidthCm = 30, HeightCm = 20 },
        },
    };

    private static OrderPickupRequest BuildPickupRequest() => new()
    {
        AuthInfo = new DhlAuthInfo { Login = "test_login", Password = "test_pass" },
        ShipmentIds = new List<string> { "SHIP-001" },
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
