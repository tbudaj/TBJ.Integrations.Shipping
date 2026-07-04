using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.FedEx.Internal;
using TBJ.Integrations.Shipping.Carriers.FedEx.Mappers;
using TBJ.Integrations.Shipping.Carriers.FedEx.Models;

namespace TBJ.Integrations.Shipping.Tests.Mappers;

/// <summary>
/// Testy jednostkowe dla <see cref="FedExMapper"/> — weryfikują budowanie żądań FedEx REST API
/// i mapowanie odpowiedzi na modele abstrakcji.
/// </summary>
public class FedExMapperTests
{
    private const string DefaultServiceType = "FEDEX_INTERNATIONAL_PRIORITY";

    private static readonly FedExAuthInfo DefaultAuthInfo = new()
    {
        ClientId = "test_client_id",
        ClientSecret = "test_secret",
        AccountNumber = "FEDEX-ACC-123",
    };

    // --- ToFedExShipRequest ---

    [Fact]
    public void ToFedExShipRequest_PoprawneDane_BudujePrawidłowąStrukturę()
    {
        // Arrange
        var request = BuildRequest();

        // Act
        var result = FedExMapper.ToFedExShipRequest(request, DefaultAuthInfo, DefaultServiceType);

        // Assert
        var shipment = result.RequestedShipment;
        Assert.NotNull(shipment);
        Assert.Equal("Firma Testowa", shipment!.Shipper?.Contact?.CompanyName);
        Assert.Equal("Odbiorca Testowy", shipment.Recipients?[0].Contact?.CompanyName);
        Assert.Equal("FEDEX-ACC-123", result.AccountNumber?.Value);
    }

    [Fact]
    public void ToFedExShipRequest_UstawiDomyślnyTypUsługi()
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
        var result = FedExMapper.ToFedExShipRequest(request, DefaultAuthInfo, DefaultServiceType);

        // Assert
        Assert.Equal("FEDEX_INTERNATIONAL_PRIORITY", result.RequestedShipment?.ServiceType);
    }

    [Fact]
    public void ToFedExShipRequest_NadpisanieTypuUsługi_UżywaKoduZZądania()
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
            ServiceCode = "FEDEX_GROUND",
        };

        // Act
        var result = FedExMapper.ToFedExShipRequest(request, DefaultAuthInfo, DefaultServiceType);

        // Assert
        Assert.Equal("FEDEX_GROUND", result.RequestedShipment?.ServiceType);
    }

    [Fact]
    public void ToFedExShipRequest_ZWymiarami_UstawiaWymiary()
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
                new() { WeightKg = 2.0m, LengthCm = 40, WidthCm = 30, HeightCm = 20 },
            },
        };

        // Act
        var result = FedExMapper.ToFedExShipRequest(request, DefaultAuthInfo, DefaultServiceType);

        // Assert
        var package = result.RequestedShipment?.RequestedPackageLineItems?[0];
        Assert.NotNull(package?.Dimensions);
        Assert.Equal(40, package!.Dimensions!.Length);
        Assert.Equal(30, package.Dimensions.Width);
        Assert.Equal(20, package.Dimensions.Height);
        Assert.Equal("CM", package.Dimensions.Units);
    }

    [Fact]
    public void ToFedExShipRequest_BezWymiarów_DimensionsNull()
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
                new() { WeightKg = 1.5m }, // brak wymiarów
            },
        };

        // Act
        var result = FedExMapper.ToFedExShipRequest(request, DefaultAuthInfo, DefaultServiceType);

        // Assert
        var package = result.RequestedShipment?.RequestedPackageLineItems?[0];
        Assert.Null(package?.Dimensions);
    }

    // --- ToShipmentResult ---

    [Fact]
    public void ToShipmentResult_ZEtykietą_ZwracaLabelResult()
    {
        // Arrange
        var labelBase64 = Convert.ToBase64String(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var resp = new FedExShipResponse
        {
            Output = new FedExShipOutput
            {
                TransactionShipments = new List<FedExTransactionShipment>
                {
                    new FedExTransactionShipment
                    {
                        MasterTrackingNumber = "7489660000000",
                        PieceResponses = new List<FedExPieceResponse>
                        {
                            new FedExPieceResponse
                            {
                                PackageDocuments = new List<FedExPackageDocument>
                                {
                                    new FedExPackageDocument { EncodedLabel = labelBase64 },
                                },
                            },
                        },
                    },
                },
            },
        };

        // Act
        var result = FedExMapper.ToShipmentResult(resp);

        // Assert
        Assert.Equal("7489660000000", result.TrackingNumber);
        Assert.Equal("7489660000000", result.CarrierShipmentId);
        Assert.NotNull(result.Label);
        Assert.Equal("application/pdf", result.Label!.ContentType);
    }

    [Fact]
    public void ToShipmentResult_BezEtykiety_LabelNull()
    {
        // Arrange
        var resp = new FedExShipResponse
        {
            Output = new FedExShipOutput
            {
                TransactionShipments = new List<FedExTransactionShipment>
                {
                    new FedExTransactionShipment { MasterTrackingNumber = "1111111111111" },
                },
            },
        };

        // Act
        var result = FedExMapper.ToShipmentResult(resp);

        // Assert
        Assert.Null(result.Label);
        Assert.Equal("1111111111111", result.TrackingNumber);
    }

    // --- ToPickupResult ---

    [Fact]
    public void ToPickupResult_ZwracaPickupConfirmationCode()
    {
        // Arrange
        var resp = new FedExPickupResponse
        {
            Output = new FedExPickupOutput
            {
                PickupConfirmationCode = "PICKUP-CONF-FEDEX-001",
                Location = "WAWPL",
            },
        };

        // Act
        var result = FedExMapper.ToPickupResult(resp);

        // Assert
        Assert.Equal("PICKUP-CONF-FEDEX-001", result.PickupOrderId);
        Assert.Equal("WAWPL", result.ConfirmedPickupWindow);
    }

    // --- ToTrackingResult (statusy FedEx) ---

    [Theory]
    [InlineData("DL", ShipmentStatus.Delivered)]
    [InlineData("IT", ShipmentStatus.InTransit)]
    [InlineData("OD", ShipmentStatus.OutForDelivery)]
    [InlineData("PU", ShipmentStatus.PickedUp)]
    [InlineData("DE", ShipmentStatus.DeliveryAttemptFailed)]
    [InlineData("RS", ShipmentStatus.ReturnedToSender)]
    [InlineData("CA", ShipmentStatus.Cancelled)]
    [InlineData("OC", ShipmentStatus.Registered)]
    [InlineData("XX", ShipmentStatus.Unknown)]
    [InlineData(null, ShipmentStatus.Unknown)]
    public void ToTrackingResult_MapujeStatusyFedExNaAbstrakcję(string? fedExStatus, ShipmentStatus expectedStatus)
    {
        // Arrange
        var resp = new FedExTrackResponse
        {
            Output = new FedExTrackOutput
            {
                CompleteTrackResults = new List<FedExCompleteTrackResult>
                {
                    new FedExCompleteTrackResult
                    {
                        TrackResults = new List<FedExTrackResult>
                        {
                            new FedExTrackResult
                            {
                                LatestStatusDetail = new FedExLatestStatusDetail
                                {
                                    DerivedCode = fedExStatus,
                                    Description = "Status testowy",
                                },
                                ScanEvents = new List<FedExScanEvent>
                                {
                                    new FedExScanEvent
                                    {
                                        DerivedStatusCode = fedExStatus,
                                        EventDescription = "Zdarzenie testowe",
                                        Date = "2024-06-15",
                                        Time = "10:00:00",
                                    },
                                },
                            },
                        },
                    },
                },
            },
        };

        // Act
        var result = FedExMapper.ToTrackingResult(resp, "7489660000001");

        // Assert
        Assert.Equal(expectedStatus, result.CurrentStatus);
        Assert.Equal("7489660000001", result.TrackingNumber);
    }

    [Fact]
    public void ToTrackingResult_ZEstimatedDelivery_UstawiaEstimatedDelivery()
    {
        // Arrange
        var resp = new FedExTrackResponse
        {
            Output = new FedExTrackOutput
            {
                CompleteTrackResults = new List<FedExCompleteTrackResult>
                {
                    new FedExCompleteTrackResult
                    {
                        TrackResults = new List<FedExTrackResult>
                        {
                            new FedExTrackResult
                            {
                                DateAndTimes = new List<FedExDateAndTime>
                                {
                                    new FedExDateAndTime { Type = "ESTIMATED_DELIVERY", DateTime = "2024-06-20T14:00:00" },
                                },
                                LatestStatusDetail = new FedExLatestStatusDetail { DerivedCode = "IT" },
                                ScanEvents = new List<FedExScanEvent>(),
                            },
                        },
                    },
                },
            },
        };

        // Act
        var result = FedExMapper.ToTrackingResult(resp, "7489660000002");

        // Assert
        Assert.NotNull(result.EstimatedDelivery);
    }

    [Fact]
    public void ToTrackingResult_BrakDanych_ZwracaUnknown()
    {
        // Arrange
        var resp = new FedExTrackResponse();

        // Act
        var result = FedExMapper.ToTrackingResult(resp, "BRAK");

        // Assert
        Assert.Equal(ShipmentStatus.Unknown, result.CurrentStatus);
        Assert.Empty(result.Events);
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
        SenderContact = new ContactInfo
        {
            Name = "Jan Kowalski",
            Phone = "+48100200300",
            CompanyName = "Firma Testowa",
        },
        RecipientAddress = new Address
        {
            Name = "Odbiorca Testowy",
            Street = "ul. Odbiorcza 2",
            PostalCode = "30-001",
            City = "Kraków",
            CountryCode = "PL",
        },
        RecipientContact = new ContactInfo
        {
            Name = "Anna Nowak",
            Phone = "+48300200100",
            CompanyName = "Odbiorca Testowy",
        },
        Parcels = new List<ParcelDimensions>
        {
            new() { WeightKg = 2.5m },
        },
    };
}
