using System.Globalization;
using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.UPS.Models;

namespace TBJ.Integrations.Shipping.Carriers.UPS.Mappers;

/// <summary>
/// Mapuje domenowe modele wysyłkowe na żądania UPS API oraz odpowiedzi API na modele wynikowe.
/// </summary>
internal static class UpsMapper
{
    /// <summary>
    /// Buduje żądanie rejestracji przesyłki w UPS Shipping API.
    /// </summary>
    /// <param name="req">Żądanie rejestracji przesyłki.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta UPS.</param>
    /// <param name="defaultServiceCode">Domyślny kod usługi UPS (gdy nie podano w żądaniu).</param>
    /// <returns>Żądanie UPS gotowe do serializacji JSON.</returns>
    public static UpsShipRequestRoot ToUpsShipRequest(CreateShipmentRequest req, UpsAuthInfo authInfo, string defaultServiceCode = "03")
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(authInfo);

        var serviceCode = req.ServiceCode ?? defaultServiceCode;

        var packages = req.Parcels.Select(p => new UpsPackage
        {
            Packaging = new UpsPackagingType { Code = "02" },
            PackageWeight = new UpsPackageWeight
            {
                UnitOfMeasurement = new UpsUnitOfMeasurement { Code = "KGS" },
                Weight = p.WeightKg.ToString("F1", CultureInfo.InvariantCulture),
            },
        }).ToList();

        return new UpsShipRequestRoot
        {
            ShipmentRequest = new UpsShipmentRequest
            {
                Shipment = new UpsShipment
                {
                    Shipper = new UpsShipper
                    {
                        Name = req.SenderAddress.Name,
                        AttentionName = req.SenderContact.Name,
                        ShipperNumber = authInfo.AccountNumber,
                        Phone = new UpsPhone { Number = req.SenderContact.Phone },
                        Address = new UpsAddress
                        {
                            AddressLine = new List<string> { req.SenderAddress.Street },
                            City = req.SenderAddress.City,
                            PostalCode = req.SenderAddress.PostalCode,
                            CountryCode = req.SenderAddress.CountryCode,
                        },
                    },
                    ShipTo = new UpsParty
                    {
                        Name = req.RecipientAddress.Name,
                        AttentionName = req.RecipientContact.Name,
                        Phone = new UpsPhone { Number = req.RecipientContact.Phone },
                        Address = new UpsAddress
                        {
                            AddressLine = new List<string> { req.RecipientAddress.Street },
                            City = req.RecipientAddress.City,
                            PostalCode = req.RecipientAddress.PostalCode,
                            CountryCode = req.RecipientAddress.CountryCode,
                        },
                    },
                    ShipFrom = new UpsParty
                    {
                        Name = req.SenderAddress.Name,
                        AttentionName = req.SenderContact.Name,
                        Phone = new UpsPhone { Number = req.SenderContact.Phone },
                        Address = new UpsAddress
                        {
                            AddressLine = new List<string> { req.SenderAddress.Street },
                            City = req.SenderAddress.City,
                            PostalCode = req.SenderAddress.PostalCode,
                            CountryCode = req.SenderAddress.CountryCode,
                        },
                    },
                    Service = new UpsService { Code = serviceCode },
                    Package = packages,
                    PaymentInformation = new UpsPaymentInformation
                    {
                        ShipmentCharge = new List<UpsShipmentCharge>
                        {
                            new UpsShipmentCharge
                            {
                                Type = "01",
                                BillShipper = new UpsBillShipper { AccountNumber = authInfo.AccountNumber },
                            },
                        },
                    },
                },
            },
        };
    }

    /// <summary>
    /// Mapuje odpowiedź UPS Shipping API na <see cref="ShipmentResult"/>.
    /// </summary>
    /// <param name="resp">Odpowiedź UPS API.</param>
    /// <returns>Wynik rejestracji przesyłki.</returns>
    public static ShipmentResult ToShipmentResult(UpsShipResponseRoot resp)
    {
        ArgumentNullException.ThrowIfNull(resp);

        var results = resp.ShipmentResponse?.ShipmentResults;
        var shipmentId = results?.ShipmentIdentificationNumber ?? string.Empty;
        var firstPkg = results?.PackageResults?.FirstOrDefault();
        var trackingNumber = firstPkg?.TrackingNumber ?? shipmentId;

        byte[]? labelBytes = null;
        string? labelContentType = "application/pdf";
        if (!string.IsNullOrWhiteSpace(firstPkg?.ShippingLabel?.GraphicImage))
        {
            labelBytes = Convert.FromBase64String(firstPkg.ShippingLabel.GraphicImage);
        }

        return new ShipmentResult
        {
            TrackingNumber = trackingNumber,
            CarrierShipmentId = shipmentId,
            Label = labelBytes != null
                ? new LabelResult
                {
                    Content = labelBytes,
                    ContentType = labelContentType,
                    FileName = $"ups_label_{trackingNumber}.pdf",
                }
                : null,
        };
    }

    /// <summary>
    /// Buduje żądanie zamówienia odbioru w UPS Pickup API.
    /// </summary>
    /// <param name="req">Żądanie zamówienia odbioru.</param>
    /// <returns>Żądanie UPS gotowe do serializacji JSON.</returns>
    public static UpsPickupRequestRoot ToUpsPickupRequest(OrderPickupRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        return new UpsPickupRequestRoot
        {
            PickupCreationRequest = new UpsPickupCreationRequest
            {
                CustomerDisplayName = req.PickupContact.Name,
                PickupDateInfo = new UpsPickupDateInfo
                {
                    PickupDate = req.PickupDate.ToString("yyyyMMdd"),
                    ReadyTime = req.PickupTimeFrom.ToString("HHmm"),
                    CloseTime = req.PickupTimeTo.ToString("HHmm"),
                },
                PickupAddress = new UpsPickupAddressInfo
                {
                    CompanyName = req.PickupAddress.Name,
                    ContactName = req.PickupContact.Name,
                    Phone = req.PickupContact.Phone,
                    Address = new UpsAddress
                    {
                        AddressLine = new List<string> { req.PickupAddress.Street },
                        City = req.PickupAddress.City,
                        PostalCode = req.PickupAddress.PostalCode,
                        CountryCode = req.PickupAddress.CountryCode,
                    },
                },
            },
        };
    }

    /// <summary>
    /// Mapuje odpowiedź UPS Pickup API na <see cref="PickupResult"/>.
    /// </summary>
    /// <param name="resp">Odpowiedź UPS Pickup API.</param>
    /// <returns>Wynik zamówienia odbioru.</returns>
    public static PickupResult ToPickupResult(UpsPickupResponseRoot resp)
    {
        ArgumentNullException.ThrowIfNull(resp);

        return new PickupResult
        {
            PickupOrderId = resp.PickupCreationResponse?.PRN ?? string.Empty,
        };
    }

    /// <summary>
    /// Mapuje odpowiedź UPS Tracking API na <see cref="TrackingResult"/>.
    /// </summary>
    /// <param name="resp">Odpowiedź UPS Tracking API.</param>
    /// <param name="trackingNumber">Numer śledzenia.</param>
    /// <returns>Wynik śledzenia przesyłki.</returns>
    public static TrackingResult ToTrackingResult(UpsTrackResponseRoot resp, string trackingNumber)
    {
        ArgumentNullException.ThrowIfNull(resp);

        var shipment = resp.trackResponse?.shipment?.FirstOrDefault();
        var activities = shipment?.activity ?? new List<UpsTrackActivity>();

        var trackingEvents = activities
            .Select(a => new TrackingEvent
            {
                OccurredAt = ParseActivityDateTime(a.date, a.time),
                Status = MapActivityStatus(a.status?.type),
                Description = a.status?.description ?? string.Empty,
                Location = a.location?.address?.city,
            })
            .OrderByDescending(e => e.OccurredAt)
            .ToList();

        var latestActivity = activities.FirstOrDefault();
        var currentStatus = MapActivityStatus(latestActivity?.status?.type);
        var currentDescription = latestActivity?.status?.description ?? string.Empty;

        DateTimeOffset? estimatedDelivery = null;
        var delDate = shipment?.deliveryDate?.FirstOrDefault(d => d.type == "DEL" || d.type == "SDD")?.date;
        if (!string.IsNullOrWhiteSpace(delDate) &&
            DateTime.TryParseExact(delDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDel))
        {
            estimatedDelivery = new DateTimeOffset(parsedDel, TimeSpan.Zero);
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
    /// Mapuje odpowiedź UPS Tracking API na <see cref="DeliveryConfirmationResult"/>.
    /// </summary>
    /// <param name="resp">Odpowiedź UPS Tracking API.</param>
    /// <param name="trackingNumber">Numer śledzenia.</param>
    /// <returns>Wynik potwierdzenia doręczenia.</returns>
    public static DeliveryConfirmationResult ToDeliveryConfirmationResult(UpsTrackResponseRoot resp, string trackingNumber)
    {
        ArgumentNullException.ThrowIfNull(resp);

        var shipment = resp.trackResponse?.shipment?.FirstOrDefault();
        var deliveredActivity = shipment?.activity?.FirstOrDefault(a => a.status?.type == "D");

        var deliveredAt = deliveredActivity != null
            ? ParseActivityDateTime(deliveredActivity.date, deliveredActivity.time)
            : DateTimeOffset.UtcNow;

        byte[]? podDocument = null;
        string? podContentType = null;
        var podContent = shipment?.deliveryInformation?.pod?.content;
        if (!string.IsNullOrWhiteSpace(podContent))
        {
            podDocument = System.Text.Encoding.UTF8.GetBytes(podContent);
            podContentType = "text/html";
        }

        return new DeliveryConfirmationResult
        {
            TrackingNumber = trackingNumber,
            DeliveredAt = deliveredAt,
            ReceivedBy = shipment?.deliveryInformation?.receivedBy,
            PodDocument = podDocument,
            PodContentType = podContentType,
        };
    }

    /// <summary>
    /// Parsuje datę i czas aktywności UPS do <see cref="DateTimeOffset"/>.
    /// </summary>
    private static DateTimeOffset ParseActivityDateTime(string? date, string? time)
    {
        if (string.IsNullOrWhiteSpace(date))
            return DateTimeOffset.UtcNow;

        var dateStr = date + (string.IsNullOrWhiteSpace(time) ? "000000" : time);
        if (DateTime.TryParseExact(dateStr, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return new DateTimeOffset(parsed, TimeSpan.Zero);

        return DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Mapuje typ aktywności UPS na znormalizowany <see cref="ShipmentStatus"/>.
    /// </summary>
    private static ShipmentStatus MapActivityStatus(string? type)
    {
        return type?.ToUpperInvariant() switch
        {
            "D" => ShipmentStatus.Delivered,
            "I" => ShipmentStatus.InTransit,
            "O" => ShipmentStatus.OutForDelivery,
            "P" => ShipmentStatus.PickedUp,
            "X" => ShipmentStatus.DeliveryAttemptFailed,
            "RS" => ShipmentStatus.ReturnedToSender,
            "M" => ShipmentStatus.Registered,
            _ => ShipmentStatus.Unknown,
        };
    }
}
