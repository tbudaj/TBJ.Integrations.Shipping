using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.UPS.Mappers;
using TBJ.Integrations.Shipping.Carriers.UPS.Models;

namespace TBJ.Integrations.Shipping.Tests.Mappers;

/// <summary>
/// Testy jednostkowe dla <see cref="UpsMapper"/> — weryfikują budowanie żądań UPS REST API
/// i mapowanie odpowiedzi na modele abstrakcji.
/// </summary>
public class UpsMapperTests
{
    private const string DefaultServiceCode = "11";

    private static readonly UpsAuthInfo DefaultAuthInfo = new()
    {
        ClientId = "test_client_id",
        ClientSecret = "test_secret",
        AccountNumber = "ACC12345",
    };

    // --- ToUpsShipRequest ---

    [Fact]
    public void ToUpsShipRequest_PoprawneDane_BudujePrawidłowąStrukturę()
    {
        // Arrange
        var request = BuildRequest();

        // Act
        var result = UpsMapper.ToUpsShipRequest(request, DefaultAuthInfo, DefaultServiceCode);

        // Assert
        var shipment = result.ShipmentRequest?.Shipment;
        Assert.NotNull(shipment);
        Assert.Equal("Firma Testowa", shipment!.Shipper?.Name);
        Assert.Equal("Odbiorca Testowy", shipment.ShipTo?.Name);
        Assert.Equal("ACC12345", shipment.Shipper?.ShipperNumber);
        Assert.Equal("ACC12345", shipment.PaymentInformation?.ShipmentCharge?[0].BillShipper?.AccountNumber);
    }

    [Fact]
    public void ToUpsShipRequest_UstawiDomyślnyKodUsługi()
    {
        // Arrange
        var request = new CreateShipmentRequest
        {
            AuthInfo = DefaultAuthInfo,
            SenderAddress = BuildRequest().SenderAddress,
            SenderContact = BuildRequest().SenderContact,
            RecipientAddress = BuildRequest().RecipientAddress,
            RecipientContact = BuildRequest().RecipientContact,
            Parcels = BuildRequest().Parcels,
            ServiceCode = null,
        };

        // Act
        var result = UpsMapper.ToUpsShipRequest(request, DefaultAuthInfo, DefaultServiceCode);

        // Assert
        Assert.Equal("11", result.ShipmentRequest?.Shipment?.Service?.Code);
    }

    [Fact]
    public void ToUpsShipRequest_NadpisanieKoduUsługi_UżywaKoduZZądania()
    {
        // Arrange
        var request = new CreateShipmentRequest
        {
            AuthInfo = DefaultAuthInfo,
            SenderAddress = BuildRequest().SenderAddress,
            SenderContact = BuildRequest().SenderContact,
            RecipientAddress = BuildRequest().RecipientAddress,
            RecipientContact = BuildRequest().RecipientContact,
            Parcels = BuildRequest().Parcels,
            ServiceCode = "07", // UPS Worldwide Express
        };

        // Act
        var result = UpsMapper.ToUpsShipRequest(request, DefaultAuthInfo, DefaultServiceCode);

        // Assert
        Assert.Equal("07", result.ShipmentRequest?.Shipment?.Service?.Code);
    }

    [Fact]
    public void ToUpsShipRequest_KilkaPaczek_TworzyListęPaczek()
    {
        // Arrange
        var request = new CreateShipmentRequest
        {
            AuthInfo = DefaultAuthInfo,
            SenderAddress = BuildRequest().SenderAddress,
            SenderContact = BuildRequest().SenderContact,
            RecipientAddress = BuildRequest().RecipientAddress,
            RecipientContact = BuildRequest().RecipientContact,
            Parcels = new List<ParcelDimensions>
            {
                new() { WeightKg = 1.5m },
                new() { WeightKg = 3.0m },
            },
        };

        // Act
        var result = UpsMapper.ToUpsShipRequest(request, DefaultAuthInfo, DefaultServiceCode);

        // Assert
        Assert.Equal(2, result.ShipmentRequest?.Shipment?.Package?.Count);
    }

    [Fact]
    public void ToUpsShipRequest_FormatterWagaWKiloOneDp()
    {
        // Arrange
        var request = new CreateShipmentRequest
        {
            AuthInfo = DefaultAuthInfo,
            SenderAddress = BuildRequest().SenderAddress,
            SenderContact = BuildRequest().SenderContact,
            RecipientAddress = BuildRequest().RecipientAddress,
            RecipientContact = BuildRequest().RecipientContact,
            Parcels = new List<ParcelDimensions> { new() { WeightKg = 2.567m } },
        };

        // Act
        var result = UpsMapper.ToUpsShipRequest(request, DefaultAuthInfo, DefaultServiceCode);

        // Assert
        Assert.Equal("2.6", result.ShipmentRequest?.Shipment?.Package?[0].PackageWeight?.Weight);
    }

    // --- ToShipmentResult ---

    [Fact]
    public void ToShipmentResult_ZEtykietą_ZwracaLabelResult()
    {
        // Arrange
        var labelBase64 = Convert.ToBase64String(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var resp = new UpsShipResponseRoot
        {
            ShipmentResponse = new UpsShipmentResponse
            {
                ShipmentResults = new UpsShipmentResults
                {
                    ShipmentIdentificationNumber = "1Z999AA1012345678",
                    PackageResults = new List<UpsPackageResult>
                    {
                        new UpsPackageResult
                        {
                            TrackingNumber = "1Z999AA1012345678",
                            ShippingLabel = new UpsShippingLabel { GraphicImage = labelBase64 },
                        },
                    },
                },
            },
        };

        // Act
        var result = UpsMapper.ToShipmentResult(resp);

        // Assert
        Assert.Equal("1Z999AA1012345678", result.TrackingNumber);
        Assert.Equal("1Z999AA1012345678", result.CarrierShipmentId);
        Assert.NotNull(result.Label);
        Assert.Equal("application/pdf", result.Label!.ContentType);
    }

    [Fact]
    public void ToShipmentResult_BezEtykiety_LabelNull()
    {
        // Arrange
        var resp = new UpsShipResponseRoot
        {
            ShipmentResponse = new UpsShipmentResponse
            {
                ShipmentResults = new UpsShipmentResults
                {
                    ShipmentIdentificationNumber = "1Z999AA1000000001",
                    PackageResults = new List<UpsPackageResult>
                    {
                        new UpsPackageResult { TrackingNumber = "1Z999AA1000000001" },
                    },
                },
            },
        };

        // Act
        var result = UpsMapper.ToShipmentResult(resp);

        // Assert
        Assert.Null(result.Label);
        Assert.Equal("1Z999AA1000000001", result.TrackingNumber);
    }

    // --- ToPickupResult ---

    [Fact]
    public void ToPickupResult_ZwracaPRN()
    {
        // Arrange
        var resp = new UpsPickupResponseRoot
        {
            PickupCreationResponse = new UpsPickupCreationResponse { PRN = "PRN-UP-98765" },
        };

        // Act
        var result = UpsMapper.ToPickupResult(resp);

        // Assert
        Assert.Equal("PRN-UP-98765", result.PickupOrderId);
    }

    // --- ToTrackingResult (statusy UPS) ---

    [Theory]
    [InlineData("D", ShipmentStatus.Delivered)]
    [InlineData("I", ShipmentStatus.InTransit)]
    [InlineData("O", ShipmentStatus.OutForDelivery)]
    [InlineData("P", ShipmentStatus.PickedUp)]
    [InlineData("X", ShipmentStatus.DeliveryAttemptFailed)]
    [InlineData("RS", ShipmentStatus.ReturnedToSender)]
    [InlineData("M", ShipmentStatus.Registered)]
    [InlineData("Z", ShipmentStatus.Unknown)]
    [InlineData(null, ShipmentStatus.Unknown)]
    public void ToTrackingResult_MapujeStatusyUPSNaAbstrakcję(string? upsStatus, ShipmentStatus expectedStatus)
    {
        // Arrange
        var resp = new UpsTrackResponseRoot
        {
            trackResponse = new UpsTrackResponse
            {
                shipment = new List<UpsTrackedShipment>
                {
                    new UpsTrackedShipment
                    {
                        activity = new List<UpsTrackActivity>
                        {
                            new UpsTrackActivity
                            {
                                status = new UpsTrackActivityStatus { type = upsStatus, description = "Opis" },
                                date = "20240615",
                                time = "100000",
                            },
                        },
                    },
                },
            },
        };

        // Act
        var result = UpsMapper.ToTrackingResult(resp, "1Z999TEST");

        // Assert
        Assert.Equal(expectedStatus, result.CurrentStatus);
    }

    [Fact]
    public void ToTrackingResult_ZwracaHistorięZdarzeń()
    {
        // Arrange
        var resp = new UpsTrackResponseRoot
        {
            trackResponse = new UpsTrackResponse
            {
                shipment = new List<UpsTrackedShipment>
                {
                    new UpsTrackedShipment
                    {
                        activity = new List<UpsTrackActivity>
                        {
                            new UpsTrackActivity
                            {
                                status = new UpsTrackActivityStatus { type = "I", description = "W drodze" },
                                date = "20240614",
                                time = "080000",
                                location = new UpsTrackLocation { address = new UpsTrackAddress { city = "Kraków" } },
                            },
                            new UpsTrackActivity
                            {
                                status = new UpsTrackActivityStatus { type = "D", description = "Doręczona" },
                                date = "20240615",
                                time = "120000",
                                location = new UpsTrackLocation { address = new UpsTrackAddress { city = "Warszawa" } },
                            },
                        },
                    },
                },
            },
        };

        // Act
        var result = UpsMapper.ToTrackingResult(resp, "1Z999TEST002");

        // Assert
        Assert.Equal("1Z999TEST002", result.TrackingNumber);
        Assert.Equal(2, result.Events.Count);
        // Posortowane malejąco po czasie — najnowsze pierwsze (Warszawa 2024-06-15 > Kraków 2024-06-14)
        Assert.Equal("Warszawa", result.Events[0].Location);
        Assert.Equal("Kraków", result.Events[1].Location);
    }

    // --- ToDeliveryConfirmationResult ---

    [Fact]
    public void ToDeliveryConfirmationResult_ZDeliveryActivity_ZwracaPrawidłowyTermin()
    {
        // Arrange
        var resp = new UpsTrackResponseRoot
        {
            trackResponse = new UpsTrackResponse
            {
                shipment = new List<UpsTrackedShipment>
                {
                    new UpsTrackedShipment
                    {
                        activity = new List<UpsTrackActivity>
                        {
                            new UpsTrackActivity
                            {
                                status = new UpsTrackActivityStatus { type = "D" },
                                date = "20240615",
                                time = "143000",
                            },
                        },
                        deliveryInformation = new UpsDeliveryInformation { receivedBy = "Jan Odbiorca" },
                    },
                },
            },
        };

        // Act
        var result = UpsMapper.ToDeliveryConfirmationResult(resp, "1Z999TEST003");

        // Assert
        Assert.Equal("1Z999TEST003", result.TrackingNumber);
        Assert.Equal("Jan Odbiorca", result.ReceivedBy);
        Assert.Equal(2024, result.DeliveredAt.Year);
        Assert.Equal(6, result.DeliveredAt.Month);
        Assert.Equal(15, result.DeliveredAt.Day);
    }

    // --- Helpers ---

    private static CreateShipmentRequest BuildRequest() => new()
    {
        AuthInfo = DefaultAuthInfo,
        SenderAddress = new Address
        {
            Name = "Firma Testowa",
            Street = "ul. Testowa 1",
            PostalCode = "00-001",
            City = "Warszawa",
            CountryCode = "PL",
        },
        SenderContact = new ContactInfo { Name = "Jan Kowalski", Phone = "+48100200300" },
        RecipientAddress = new Address
        {
            Name = "Odbiorca Testowy",
            Street = "ul. Odbiorcza 2",
            PostalCode = "30-001",
            City = "Kraków",
            CountryCode = "PL",
        },
        RecipientContact = new ContactInfo { Name = "Anna Nowak", Phone = "+48300200100" },
        Parcels = new List<ParcelDimensions>
        {
            new() { WeightKg = 2.5m },
        },
        Reference = "ZAM-UPS-001",
    };
}
