using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.DPD.Mappers;

namespace TBJ.Integrations.Shipping.Tests.Mappers;

/// <summary>
/// Testy jednostkowe dla <see cref="DpdMapper"/> — weryfikują poprawność budowania XML
/// i mapowania odpowiedzi DPD na modele abstrakcji.
/// </summary>
public class DpdMapperTests
{
    // --- BuildOpenUmlf ---

    [Fact]
    public void BuildOpenUmlf_PoprawneDane_ZawieraWymaganeElementy()
    {
        // Arrange
        var request = BuildRequest();

        // Act
        var xml = DpdMapper.BuildOpenUmlf(request);

        // Assert
        Assert.Contains("<ns:openUMLF>", xml);
        Assert.Contains("Firma Testowa", xml);
        Assert.Contains("Odbiorca Testowy", xml);
        Assert.Contains("00-001", xml);
        Assert.Contains("30-001", xml);
        Assert.Contains("2.5", xml);
    }

    [Fact]
    public void BuildOpenUmlf_ZPobraniem_ZawieraElementCod()
    {
        // Arrange
        var requestWithCod = new CreateShipmentRequest
        {
            AuthInfo = new DpdAuthInfo { Username = "u", Password = "p", Fid = 1 },
            SenderAddress = new Address { Name = "Firma Testowa", Street = "ul. Testowa 1", PostalCode = "00-001", City = "Warszawa" },
            SenderContact = new ContactInfo { Name = "Adam Nowak", Phone = "+48123456789" },
            RecipientAddress = new Address { Name = "Odbiorca Testowy", Street = "ul. Odbiorcza 2", PostalCode = "30-001", City = "Kraków" },
            RecipientContact = new ContactInfo { Name = "Maria Kowalska", Phone = "+48987654321" },
            Parcels = new List<ParcelDimensions> { new() { WeightKg = 2.5m } },
            Cod = new CodInfo { AmountPln = 150m, BankAccountIban = "PL61109010140000071219812874" },
        };

        // Act
        var xml = DpdMapper.BuildOpenUmlf(requestWithCod);

        // Assert
        Assert.Contains("<ns:cod>", xml);
        Assert.Contains("150", xml);
    }

    [Fact]
    public void BuildOpenUmlf_BezUbezpieczenia_NieZawieraElementuInsurance()
    {
        // Arrange
        var request = BuildRequest();

        // Act
        var xml = DpdMapper.BuildOpenUmlf(request);

        // Assert
        Assert.DoesNotContain("<ns:insurance>", xml);
    }

    // --- ToShipmentResult ---

    [Fact]
    public void ToShipmentResult_ZEtykietą_ZwracaLabelResult()
    {
        // Arrange
        var labelBytes = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var result = DpdMapper.ToShipmentResult("PKG-001", "WAYBILL-001", labelBytes);

        // Assert
        Assert.Equal("WAYBILL-001", result.TrackingNumber);
        Assert.Equal("PKG-001", result.CarrierShipmentId);
        Assert.NotNull(result.Label);
        Assert.Equal("application/pdf", result.Label!.ContentType);
        Assert.Equal(labelBytes, result.Label.Content);
    }

    [Fact]
    public void ToShipmentResult_BezEtykiety_LabelNull()
    {
        // Act
        var result = DpdMapper.ToShipmentResult("PKG-001", "WAYBILL-001", null);

        // Assert
        Assert.Null(result.Label);
    }

    // --- ToPickupResult ---

    [Fact]
    public void ToPickupResult_ZwracaPoprawneDaneDyspozycji()
    {
        // Act
        var result = DpdMapper.ToPickupResult("DOC-123");

        // Assert
        Assert.Equal("DOC-123", result.PickupOrderId);
    }

    // --- ToTrackingResult (status mapping) ---

    [Theory]
    [InlineData("D", ShipmentStatus.Delivered)]
    [InlineData("d", ShipmentStatus.Delivered)]
    [InlineData("P", ShipmentStatus.InTransit)]
    [InlineData("T", ShipmentStatus.InTransit)]
    [InlineData("B", ShipmentStatus.ReturnedToSender)]
    [InlineData("C", ShipmentStatus.PickedUp)]
    [InlineData("X", ShipmentStatus.Unknown)]
    [InlineData("", ShipmentStatus.Unknown)]
    public void ToTrackingResult_MapujeStatusyDPDNaAbstrakcję(string dpdStatus, ShipmentStatus expectedStatus)
    {
        // Act
        var result = DpdMapper.ToTrackingResult("123", dpdStatus, "Opis statusu", DateTime.UtcNow);

        // Assert
        Assert.Equal(expectedStatus, result.CurrentStatus);
    }

    [Fact]
    public void ToTrackingResult_ZwracaHistorięZdarzeń()
    {
        // Arrange
        var eventTime = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = DpdMapper.ToTrackingResult("WAYBILL-001", "D", "Doręczona", eventTime);

        // Assert
        Assert.Equal("WAYBILL-001", result.TrackingNumber);
        Assert.Single(result.Events);
        Assert.Equal(ShipmentStatus.Delivered, result.Events[0].Status);
    }

    // --- ToDeliveryConfirmationResult ---

    [Fact]
    public void ToDeliveryConfirmationResult_ZwracaPoprawneDane()
    {
        // Arrange
        var deliveredAt = DateTime.UtcNow.AddHours(-3);

        // Act
        var result = DpdMapper.ToDeliveryConfirmationResult("WAYBILL-001", "Doręczona", deliveredAt);

        // Assert
        Assert.Equal("WAYBILL-001", result.TrackingNumber);
        Assert.Equal(deliveredAt, result.DeliveredAt.UtcDateTime);
    }

    // --- Helpers ---

    private static CreateShipmentRequest BuildRequest() => new()
    {
        AuthInfo = new DpdAuthInfo { Username = "test_user", Password = "test_pass", Fid = 12345 },
        SenderAddress = new Address
        {
            Name = "Firma Testowa",
            Street = "ul. Testowa",
            BuildingNumber = "1",
            PostalCode = "00-001",
            City = "Warszawa",
            Nip = "1234567890",
        },
        SenderContact = new ContactInfo { Name = "Adam Nowak", Phone = "+48123456789", Email = "adam@test.pl" },
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
            new() { WeightKg = 2.5m, LengthCm = 30, WidthCm = 20, HeightCm = 15 },
        },
        Reference = "ZAM-2024-001",
    };
}
