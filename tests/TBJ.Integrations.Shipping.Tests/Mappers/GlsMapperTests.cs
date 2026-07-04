using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.GLS.Configuration;
using TBJ.Integrations.Shipping.Carriers.GLS.Mappers;

namespace TBJ.Integrations.Shipping.Tests.Mappers;

/// <summary>
/// Testy jednostkowe dla <see cref="GlsMapper"/> — weryfikują poprawność budowania XML
/// i mapowania odpowiedzi GLS ADE-Plus na modele abstrakcji.
/// </summary>
public class GlsMapperTests
{
    private static readonly GlsOptions DefaultOptions = new()
    {
        BaseUrl = "https://ade.gls-poland.com/adeplus/pm1/ade_webapi2.php",
        CountryCode = "PL",
    };

    // --- BuildConsignmentXml ---

    [Fact]
    public void BuildConsignmentXml_PoprawneDane_ZawieraWymaganeElementy()
    {
        // Arrange
        var request = BuildRequest();

        // Act
        var xml = GlsMapper.BuildConsignmentXml(request, DefaultOptions);

        // Assert
        Assert.Contains("Firma Testowa", xml);
        Assert.Contains("Odbiorca Testowy", xml);
        Assert.Contains("00-001", xml);
        Assert.Contains("30-001", xml);
        Assert.Contains("2.500", xml);
    }

    [Fact]
    public void BuildConsignmentXml_ZPobraniem_ZawieraElementCOD()
    {
        // Arrange
        var request = new CreateShipmentRequest
        {
            AuthInfo = new GlsAuthInfo { Username = "u", Password = "p" },
            SenderAddress = new Address { Name = "Firma", Street = "ul. Testowa 1", PostalCode = "00-001", City = "Warszawa" },
            SenderContact = new ContactInfo { Name = "Jan Kowalski", Phone = "+48100200300" },
            RecipientAddress = new Address { Name = "Odbiorca", Street = "ul. Odbiorcza 2", PostalCode = "30-001", City = "Kraków" },
            RecipientContact = new ContactInfo { Name = "Anna Nowak", Phone = "+48300200100" },
            Parcels = new List<ParcelDimensions> { new() { WeightKg = 3.0m } },
            Cod = new CodInfo { AmountPln = 299.99m, BankAccountIban = "PL61109010140000071219812874" },
        };

        // Act
        var xml = GlsMapper.BuildConsignmentXml(request, DefaultOptions);

        // Assert
        Assert.Contains("<ade:COD>", xml);
        Assert.Contains("299.99", xml);
    }

    [Fact]
    public void BuildConsignmentXml_BezCod_NieZawieraElementuCOD()
    {
        // Arrange
        var request = BuildRequest();

        // Act
        var xml = GlsMapper.BuildConsignmentXml(request, DefaultOptions);

        // Assert
        Assert.DoesNotContain("<ade:COD>", xml);
    }

    [Fact]
    public void BuildConsignmentXml_WielePaczek_ZawieraKilkaElementowParcel()
    {
        // Arrange
        var request = new CreateShipmentRequest
        {
            AuthInfo = new GlsAuthInfo { Username = "u", Password = "p" },
            SenderAddress = BuildRequest().SenderAddress,
            SenderContact = BuildRequest().SenderContact,
            RecipientAddress = BuildRequest().RecipientAddress,
            RecipientContact = BuildRequest().RecipientContact,
            Parcels = new List<ParcelDimensions>
            {
                new() { WeightKg = 1.0m },
                new() { WeightKg = 2.0m },
                new() { WeightKg = 3.0m },
            },
        };

        // Act
        var xml = GlsMapper.BuildConsignmentXml(request, DefaultOptions);

        // Assert
        Assert.Equal(3, CountOccurrences(xml, "<ade:Parcel>"));
    }

    [Fact]
    public void BuildConsignmentXml_ZReferencją_ZawieraRef1()
    {
        // Arrange
        var request = new CreateShipmentRequest
        {
            AuthInfo = new GlsAuthInfo { Username = "u", Password = "p" },
            SenderAddress = BuildRequest().SenderAddress,
            SenderContact = BuildRequest().SenderContact,
            RecipientAddress = BuildRequest().RecipientAddress,
            RecipientContact = BuildRequest().RecipientContact,
            Parcels = BuildRequest().Parcels,
            Reference = "ZAM-2024-GLS-001",
        };

        // Act
        var xml = GlsMapper.BuildConsignmentXml(request, DefaultOptions);

        // Assert
        Assert.Contains("ZAM-2024-GLS-001", xml);
    }

    // --- ToShipmentResult ---

    [Fact]
    public void ToShipmentResult_ZEtykietą_ZwracaLabelResult()
    {
        // Arrange
        var labelBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF

        // Act
        var result = GlsMapper.ToShipmentResult("CONS-001", "TRK-001", labelBytes);

        // Assert
        Assert.Equal("TRK-001", result.TrackingNumber);
        Assert.Equal("CONS-001", result.CarrierShipmentId);
        Assert.NotNull(result.Label);
        Assert.Equal("application/pdf", result.Label!.ContentType);
        Assert.Equal(labelBytes, result.Label.Content);
        Assert.Contains("CONS-001", result.Label.FileName);
    }

    [Fact]
    public void ToShipmentResult_BezTrackingNumber_UzywaConsignmentId()
    {
        // Act
        var result = GlsMapper.ToShipmentResult("CONS-999", null, null);

        // Assert
        Assert.Equal("CONS-999", result.TrackingNumber);
        Assert.Equal("CONS-999", result.CarrierShipmentId);
        Assert.Null(result.Label);
    }

    // --- ToPickupResult ---

    [Fact]
    public void ToPickupResult_ZwracaPoprawneDane()
    {
        // Act
        var result = GlsMapper.ToPickupResult("PICKUP-CONF-123");

        // Assert
        Assert.Equal("PICKUP-CONF-123", result.PickupOrderId);
    }

    // --- ToTrackingResult (statusy GLS) ---

    [Theory]
    [InlineData("TRANSIT", ShipmentStatus.InTransit)]
    [InlineData("INTRANSIT", ShipmentStatus.InTransit)]
    [InlineData("INWAREHOUSE", ShipmentStatus.InTransit)]
    [InlineData("OUTFORDELIVERY", ShipmentStatus.OutForDelivery)]
    [InlineData("DELIVERED", ShipmentStatus.Delivered)]
    [InlineData("NOTDELIVERED", ShipmentStatus.DeliveryAttemptFailed)]
    [InlineData("PICKUPED", ShipmentStatus.PickedUp)]
    [InlineData("RETURNED", ShipmentStatus.ReturnedToSender)]
    [InlineData("CANCELLED", ShipmentStatus.Cancelled)]
    [InlineData("NIEZNANY", ShipmentStatus.Unknown)]
    [InlineData("", ShipmentStatus.Unknown)]
    public void ToTrackingResult_MapujeStatusyGLSNaAbstrakcję(string glsStatus, ShipmentStatus expectedStatus)
    {
        // Arrange
        var events = new List<(string status, string description, DateTimeOffset time, string? location)>
        {
            (glsStatus, "Opis zdarzenia GLS", DateTimeOffset.UtcNow, "Warszawa"),
        };

        // Act
        var result = GlsMapper.ToTrackingResult("TRK-GLS-001", events);

        // Assert
        Assert.Equal(expectedStatus, result.CurrentStatus);
    }

    [Fact]
    public void ToTrackingResult_WieleZdarzeń_ZwracaSortowanePoPrzezeWydarzeniach()
    {
        // Arrange
        var older = DateTimeOffset.UtcNow.AddHours(-5);
        var newer = DateTimeOffset.UtcNow.AddHours(-1);
        var events = new List<(string status, string description, DateTimeOffset time, string? location)>
        {
            ("TRANSIT", "Odebranie w magazynie", older, "Kraków"),
            ("OUTFORDELIVERY", "W drodze do odbiorcy", newer, "Warszawa"),
        };

        // Act
        var result = GlsMapper.ToTrackingResult("TRK-GLS-002", events);

        // Assert
        Assert.Equal(2, result.Events.Count);
        Assert.Equal(newer, result.Events[0].OccurredAt);
        Assert.Equal(ShipmentStatus.OutForDelivery, result.CurrentStatus);
    }

    [Fact]
    public void ToTrackingResult_BrakZdarzeń_ZwracaUnknownStatus()
    {
        // Arrange
        var events = new List<(string status, string description, DateTimeOffset time, string? location)>();

        // Act
        var result = GlsMapper.ToTrackingResult("TRK-GLS-003", events);

        // Assert
        Assert.Equal(ShipmentStatus.Unknown, result.CurrentStatus);
        Assert.Empty(result.Events);
    }

    // --- ToDeliveryConfirmationResult ---

    [Fact]
    public void ToDeliveryConfirmationResult_ZwracaPoprawneDane()
    {
        // Arrange
        var deliveredAt = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);

        // Act
        var result = GlsMapper.ToDeliveryConfirmationResult("TRK-GLS-004", deliveredAt, "Jan Odbiorca");

        // Assert
        Assert.Equal("TRK-GLS-004", result.TrackingNumber);
        Assert.Equal(deliveredAt, result.DeliveredAt);
        Assert.Equal("Jan Odbiorca", result.ReceivedBy);
    }

    [Fact]
    public void ToDeliveryConfirmationResult_BezOdbiorcy_ReceivedByNull()
    {
        // Arrange
        var deliveredAt = DateTimeOffset.UtcNow;

        // Act
        var result = GlsMapper.ToDeliveryConfirmationResult("TRK-GLS-005", deliveredAt, null);

        // Assert
        Assert.Null(result.ReceivedBy);
    }

    // --- Helpers ---

    private static CreateShipmentRequest BuildRequest() => new()
    {
        AuthInfo = new GlsAuthInfo { Username = "test_user", Password = "test_pass" },
        SenderAddress = new Address
        {
            Name = "Firma Testowa",
            Street = "ul. Testowa",
            BuildingNumber = "1",
            PostalCode = "00-001",
            City = "Warszawa",
            CountryCode = "PL",
        },
        SenderContact = new ContactInfo { Name = "Jan Kowalski", Phone = "+48100200300", Email = "jan@test.pl" },
        RecipientAddress = new Address
        {
            Name = "Odbiorca Testowy",
            Street = "ul. Odbiorcza",
            BuildingNumber = "5",
            PostalCode = "30-001",
            City = "Kraków",
            CountryCode = "PL",
        },
        RecipientContact = new ContactInfo { Name = "Anna Nowak", Phone = "+48300200100" },
        Parcels = new List<ParcelDimensions>
        {
            new() { WeightKg = 2.5m, LengthCm = 40, WidthCm = 30, HeightCm = 20 },
        },
        Reference = "ZAM-GLS-001",
    };

    private static int CountOccurrences(string source, string substring)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(substring, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }
}
