using System.Globalization;
using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.FedEx.Models;

namespace TBJ.Integrations.Shipping.Carriers.FedEx.Mappers;

/// <summary>
/// Mapuje domenowe modele wysyłkowe na żądania FedEx API oraz odpowiedzi API na modele wynikowe.
/// </summary>
internal static class FedExMapper
{
    /// <summary>
    /// Buduje żądanie rejestracji przesyłki w FedEx Ship API.
    /// </summary>
    /// <param name="req">Żądanie rejestracji przesyłki.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta FedEx.</param>
    /// <param name="defaultServiceType">Domyślny typ usługi FedEx (gdy nie podano w żądaniu).</param>
    /// <returns>Żądanie FedEx gotowe do serializacji JSON.</returns>
    public static FedExShipRequest ToFedExShipRequest(CreateShipmentRequest req, FedExAuthInfo authInfo, string defaultServiceType = "INTERNATIONAL_ECONOMY")
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(authInfo);

        var serviceType = req.ServiceCode ?? defaultServiceType;

        var packages = req.Parcels.Select(p => new FedExRequestedPackage
        {
            Weight = new FedExWeight
            {
                Units = "KG",
                Value = p.WeightKg.ToString("F3", CultureInfo.InvariantCulture),
            },
            Dimensions = (p.LengthCm.HasValue && p.WidthCm.HasValue && p.HeightCm.HasValue)
                ? new FedExDimensions
                {
                    Length = (int)p.LengthCm.Value,
                    Width = (int)p.WidthCm.Value,
                    Height = (int)p.HeightCm.Value,
                    Units = "CM",
                }
                : null,
        }).ToList();

        return new FedExShipRequest
        {
            AccountNumber = new FedExAccountNumber { Value = authInfo.AccountNumber },
            RequestedShipment = new FedExRequestedShipment
            {
                PickupType = "USE_SCHEDULED_PICKUP",
                ServiceType = serviceType,
                LabelSpecification = new FedExLabelSpecification
                {
                    LabelFormatType = "COMMON2D",
                    LabelFileType = "PDF",
                    ImageType = "PDF",
                },
                ShippingChargesPayment = new FedExShippingChargesPayment
                {
                    PaymentType = "SENDER",
                    Payor = new FedExPayor
                    {
                        ResponsibleParty = new FedExResponsibleParty
                        {
                            AccountNumber = new FedExAccountNumber { Value = authInfo.AccountNumber },
                        },
                    },
                },
                Shipper = new FedExShipParty
                {
                    Contact = new FedExContact
                    {
                        PersonName = req.SenderContact.Name,
                        PhoneNumber = req.SenderContact.Phone,
                        CompanyName = req.SenderContact.CompanyName ?? req.SenderAddress.Name,
                    },
                    Address = new FedExShipAddress
                    {
                        StreetLines = new List<string> { req.SenderAddress.Street },
                        City = req.SenderAddress.City,
                        PostalCode = req.SenderAddress.PostalCode,
                        CountryCode = req.SenderAddress.CountryCode,
                    },
                },
                Recipients = new List<FedExShipParty>
                {
                    new FedExShipParty
                    {
                        Contact = new FedExContact
                        {
                            PersonName = req.RecipientContact.Name,
                            PhoneNumber = req.RecipientContact.Phone,
                            CompanyName = req.RecipientContact.CompanyName ?? req.RecipientAddress.Name,
                        },
                        Address = new FedExShipAddress
                        {
                            StreetLines = new List<string> { req.RecipientAddress.Street },
                            City = req.RecipientAddress.City,
                            PostalCode = req.RecipientAddress.PostalCode,
                            CountryCode = req.RecipientAddress.CountryCode,
                        },
                    },
                },
                RequestedPackageLineItems = packages,
            },
        };
    }

    /// <summary>
    /// Mapuje odpowiedź FedEx Ship API na <see cref="ShipmentResult"/>.
    /// </summary>
    /// <param name="resp">Odpowiedź FedEx Ship API.</param>
    /// <returns>Wynik rejestracji przesyłki.</returns>
    public static ShipmentResult ToShipmentResult(FedExShipResponse resp)
    {
        ArgumentNullException.ThrowIfNull(resp);

        var firstShipment = resp.Output?.TransactionShipments?.FirstOrDefault();
        var masterTrackingNumber = firstShipment?.MasterTrackingNumber ?? string.Empty;

        var firstDoc = firstShipment?.PieceResponses?.FirstOrDefault()?.PackageDocuments?.FirstOrDefault();
        byte[]? labelBytes = null;
        if (!string.IsNullOrWhiteSpace(firstDoc?.EncodedLabel))
        {
            labelBytes = Convert.FromBase64String(firstDoc.EncodedLabel);
        }

        return new ShipmentResult
        {
            TrackingNumber = masterTrackingNumber,
            CarrierShipmentId = masterTrackingNumber,
            Label = labelBytes != null
                ? new LabelResult
                {
                    Content = labelBytes,
                    ContentType = "application/pdf",
                    FileName = $"fedex_label_{masterTrackingNumber}.pdf",
                }
                : null,
        };
    }

    /// <summary>
    /// Buduje żądanie zamówienia odbioru w FedEx Pickup API.
    /// </summary>
    /// <param name="req">Żądanie zamówienia odbioru.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta FedEx.</param>
    /// <returns>Żądanie FedEx gotowe do serializacji JSON.</returns>
    public static FedExPickupRequest ToFedExPickupRequest(OrderPickupRequest req, FedExAuthInfo authInfo)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(authInfo);

        var readyDateTime = req.PickupDate.ToDateTime(req.PickupTimeFrom, DateTimeKind.Local);
        var readyDateTimestamp = readyDateTime.ToString("yyyy-MM-ddTHH:mm:ss");

        return new FedExPickupRequest
        {
            PickupServiceCategory = "FEDEX_EXPRESS",
            CarrierCode = "FDXE",
            AssociatedAccountNumber = new FedExAccountNumber { Value = authInfo.AccountNumber },
            OriginDetail = new FedExPickupOriginDetail
            {
                PickupAddressType = "ACCOUNT",
                ReadyDateTimestamp = readyDateTimestamp,
                CustomerCloseTime = req.PickupTimeTo.ToString("HH:mm"),
                PickupLocation = new FedExPickupLocation
                {
                    Contact = new FedExContact
                    {
                        PersonName = req.PickupContact.Name,
                        PhoneNumber = req.PickupContact.Phone,
                        CompanyName = req.PickupContact.CompanyName ?? req.PickupAddress.Name,
                    },
                    Address = new FedExShipAddress
                    {
                        StreetLines = new List<string> { req.PickupAddress.Street },
                        City = req.PickupAddress.City,
                        PostalCode = req.PickupAddress.PostalCode,
                        CountryCode = req.PickupAddress.CountryCode,
                    },
                },
            },
        };
    }

    /// <summary>
    /// Mapuje odpowiedź FedEx Pickup API na <see cref="PickupResult"/>.
    /// </summary>
    /// <param name="resp">Odpowiedź FedEx Pickup API.</param>
    /// <returns>Wynik zamówienia odbioru.</returns>
    public static PickupResult ToPickupResult(FedExPickupResponse resp)
    {
        ArgumentNullException.ThrowIfNull(resp);

        return new PickupResult
        {
            PickupOrderId = resp.Output?.PickupConfirmationCode ?? string.Empty,
            ConfirmedPickupWindow = resp.Output?.Location,
        };
    }

    /// <summary>
    /// Mapuje odpowiedź FedEx Track API na <see cref="TrackingResult"/>.
    /// </summary>
    /// <param name="resp">Odpowiedź FedEx Track API.</param>
    /// <param name="trackingNumber">Numer śledzenia.</param>
    /// <returns>Wynik śledzenia przesyłki.</returns>
    public static TrackingResult ToTrackingResult(FedExTrackResponse resp, string trackingNumber)
    {
        ArgumentNullException.ThrowIfNull(resp);

        var trackResult = resp.Output?.CompleteTrackResults?.FirstOrDefault()?.TrackResults?.FirstOrDefault();
        var scanEvents = trackResult?.ScanEvents ?? new List<FedExScanEvent>();

        var trackingEvents = scanEvents
            .Select(e => new TrackingEvent
            {
                OccurredAt = ParseScanEventDateTime(e.Date, e.Time),
                Status = MapStatus(e.DerivedStatusCode ?? e.EventType),
                Description = e.EventDescription ?? string.Empty,
                Location = e.ScanLocation?.City,
            })
            .OrderByDescending(e => e.OccurredAt)
            .ToList();

        var latestStatusDetail = trackResult?.LatestStatusDetail;
        var currentStatus = MapStatus(latestStatusDetail?.DerivedCode ?? latestStatusDetail?.Code);
        var currentDescription = latestStatusDetail?.Description ?? string.Empty;

        DateTimeOffset? estimatedDelivery = null;
        var estimatedDel = trackResult?.DateAndTimes?
            .FirstOrDefault(d => d.Type == "ESTIMATED_DELIVERY")?.DateTime;
        if (!string.IsNullOrWhiteSpace(estimatedDel) &&
            DateTimeOffset.TryParse(estimatedDel, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEst))
        {
            estimatedDelivery = parsedEst;
        }

        return new TrackingResult
        {
            TrackingNumber = trackingNumber,
            CurrentStatus = currentStatus,
            StatusDescription = currentDescription,
            EstimatedDelivery = estimatedDelivery,
            Events = trackingEvents,
        };
    }

    /// <summary>
    /// Mapuje odpowiedź FedEx Track API na <see cref="DeliveryConfirmationResult"/>.
    /// </summary>
    /// <param name="resp">Odpowiedź FedEx Track API.</param>
    /// <param name="trackingNumber">Numer śledzenia.</param>
    /// <returns>Wynik potwierdzenia doręczenia.</returns>
    public static DeliveryConfirmationResult ToDeliveryConfirmationResult(FedExTrackResponse resp, string trackingNumber)
    {
        ArgumentNullException.ThrowIfNull(resp);

        var trackResult = resp.Output?.CompleteTrackResults?.FirstOrDefault()?.TrackResults?.FirstOrDefault();
        var scanEvents = trackResult?.ScanEvents ?? new List<FedExScanEvent>();

        // Szukamy zdarzenia doręczenia (DL = Delivered)
        var deliveredEvent = scanEvents.FirstOrDefault(e =>
            e.DerivedStatusCode?.ToUpperInvariant() == "DL" ||
            e.EventType?.ToUpperInvariant() == "DL");

        var deliveredAt = deliveredEvent != null
            ? ParseScanEventDateTime(deliveredEvent.Date, deliveredEvent.Time)
            : DateTimeOffset.UtcNow;

        // Szukamy informacji o odbiorcy z podpisu
        var sigInfo = trackResult?.SignatureInformation;
        var receivedBy = sigInfo?.FormattedSignatureFileName;

        return new DeliveryConfirmationResult
        {
            TrackingNumber = trackingNumber,
            DeliveredAt = deliveredAt,
            ReceivedBy = receivedBy,
        };
    }

    /// <summary>
    /// Parsuje datę i czas zdarzenia skanowania FedEx do <see cref="DateTimeOffset"/>.
    /// </summary>
    private static DateTimeOffset ParseScanEventDateTime(string? date, string? time)
    {
        if (string.IsNullOrWhiteSpace(date))
            return DateTimeOffset.UtcNow;

        var dateStr = date + (string.IsNullOrWhiteSpace(time) ? "T00:00:00" : $"T{time}");
        if (DateTimeOffset.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        return DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Mapuje kod statusu FedEx na znormalizowany <see cref="ShipmentStatus"/>.
    /// </summary>
    private static ShipmentStatus MapStatus(string? code)
    {
        return code?.ToUpperInvariant() switch
        {
            "DL" => ShipmentStatus.Delivered,
            "IT" => ShipmentStatus.InTransit,
            "OD" => ShipmentStatus.OutForDelivery,
            "PU" => ShipmentStatus.PickedUp,
            "DE" => ShipmentStatus.DeliveryAttemptFailed,
            "RS" => ShipmentStatus.ReturnedToSender,
            "CA" => ShipmentStatus.Cancelled,
            "OC" => ShipmentStatus.Registered,
            _ => ShipmentStatus.Unknown,
        };
    }
}
