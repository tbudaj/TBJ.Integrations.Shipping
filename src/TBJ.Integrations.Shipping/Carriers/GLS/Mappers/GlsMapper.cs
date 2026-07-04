using System.Globalization;
using System.Net;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.GLS.Configuration;

namespace TBJ.Integrations.Shipping.Carriers.GLS.Mappers;

/// <summary>
/// Mapuje domenowe modele wysyłkowe na XML GLS oraz odpowiedzi XML na modele wynikowe.
/// </summary>
internal static class GlsMapper
{
    /// <summary>
    /// Buduje fragment XML dla operacji <c>adePrepareConsignments</c>.
    /// Zawiera dane nadawcy, odbiorcy i paczek przesyłki.
    /// </summary>
    /// <param name="req">Żądanie rejestracji przesyłki.</param>
    /// <param name="opts">Opcje konfiguracyjne GLS.</param>
    /// <returns>Fragment XML z danymi przesyłki.</returns>
    public static string BuildConsignmentXml(CreateShipmentRequest req, GlsOptions opts)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(opts);

        var senderStreet = BuildStreet(req.SenderAddress);
        var recipientStreet = BuildStreet(req.RecipientAddress);

        var parcelsXml = string.Join(
            string.Empty,
            req.Parcels.Select(p => $@"
<ade:Parcel>
    <ade:Weight>{p.WeightKg.ToString("F3", CultureInfo.InvariantCulture)}</ade:Weight>
    {(p.LengthCm.HasValue ? $"<ade:Length>{p.LengthCm.Value.ToString(CultureInfo.InvariantCulture)}</ade:Length>" : string.Empty)}
    {(p.WidthCm.HasValue ? $"<ade:Width>{p.WidthCm.Value.ToString(CultureInfo.InvariantCulture)}</ade:Width>" : string.Empty)}
    {(p.HeightCm.HasValue ? $"<ade:Height>{p.HeightCm.Value.ToString(CultureInfo.InvariantCulture)}</ade:Height>" : string.Empty)}
</ade:Parcel>"));

        var codXml = req.Cod != null
            ? $@"
<ade:COD>
    <ade:CODAmount>{req.Cod.AmountPln.ToString("F2", CultureInfo.InvariantCulture)}</ade:CODAmount>
    <ade:CODIban>{WebUtility.HtmlEncode(req.Cod.BankAccountIban)}</ade:CODIban>
    <ade:CODRef>{WebUtility.HtmlEncode(req.Cod.Reference ?? req.Reference ?? string.Empty)}</ade:CODRef>
</ade:COD>"
            : string.Empty;

        return $@"
<ade:Consignment>
    <ade:Sender>
        <ade:Name1>{WebUtility.HtmlEncode(req.SenderAddress.Name)}</ade:Name1>
        <ade:Name2>{WebUtility.HtmlEncode(req.SenderContact.Name)}</ade:Name2>
        <ade:Name3>{WebUtility.HtmlEncode(req.SenderContact.CompanyName ?? string.Empty)}</ade:Name3>
        <ade:Street>{WebUtility.HtmlEncode(senderStreet)}</ade:Street>
        <ade:CountryCode>{WebUtility.HtmlEncode(req.SenderAddress.CountryCode)}</ade:CountryCode>
        <ade:ZIPCode>{WebUtility.HtmlEncode(req.SenderAddress.PostalCode)}</ade:ZIPCode>
        <ade:City>{WebUtility.HtmlEncode(req.SenderAddress.City)}</ade:City>
        <ade:Phone>{WebUtility.HtmlEncode(req.SenderContact.Phone)}</ade:Phone>
        <ade:Email>{WebUtility.HtmlEncode(req.SenderContact.Email ?? string.Empty)}</ade:Email>
    </ade:Sender>
    <ade:Consignee>
        <ade:Name1>{WebUtility.HtmlEncode(req.RecipientAddress.Name)}</ade:Name1>
        <ade:Name2>{WebUtility.HtmlEncode(req.RecipientContact.Name)}</ade:Name2>
        <ade:Name3>{WebUtility.HtmlEncode(req.RecipientContact.CompanyName ?? string.Empty)}</ade:Name3>
        <ade:Street>{WebUtility.HtmlEncode(recipientStreet)}</ade:Street>
        <ade:CountryCode>{WebUtility.HtmlEncode(req.RecipientAddress.CountryCode)}</ade:CountryCode>
        <ade:ZIPCode>{WebUtility.HtmlEncode(req.RecipientAddress.PostalCode)}</ade:ZIPCode>
        <ade:City>{WebUtility.HtmlEncode(req.RecipientAddress.City)}</ade:City>
        <ade:Phone>{WebUtility.HtmlEncode(req.RecipientContact.Phone)}</ade:Phone>
        <ade:Email>{WebUtility.HtmlEncode(req.RecipientContact.Email ?? string.Empty)}</ade:Email>
    </ade:Consignee>
    <ade:Parcels>{parcelsXml}
    </ade:Parcels>
    {codXml}
    <ade:References>
        <ade:Ref1>{WebUtility.HtmlEncode(req.Reference ?? string.Empty)}</ade:Ref1>
        <ade:Remark>{WebUtility.HtmlEncode(req.Comment ?? string.Empty)}</ade:Remark>
    </ade:References>
</ade:Consignment>";
    }

    /// <summary>
    /// Buduje fragment XML dla operacji <c>adePickup_CallPickup</c>.
    /// </summary>
    /// <param name="req">Żądanie zamówienia odbioru.</param>
    /// <param name="consignmentIds">Lista identyfikatorów przesyłek GLS do odebrania.</param>
    /// <param name="opts">Opcje konfiguracyjne GLS.</param>
    /// <returns>Fragment XML z danymi dyspozycji odbioru.</returns>
    public static string BuildPickupXml(OrderPickupRequest req, IReadOnlyList<string> consignmentIds, GlsOptions opts)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(consignmentIds);

        var street = BuildStreet(req.PickupAddress);
        var consignIdsXml = string.Join(
            string.Empty,
            consignmentIds.Select(id => $"<ade:ConsignId>{WebUtility.HtmlEncode(id)}</ade:ConsignId>"));

        return $@"
<ade:PickupAddress>
    <ade:Name1>{WebUtility.HtmlEncode(req.PickupAddress.Name)}</ade:Name1>
    <ade:Name2>{WebUtility.HtmlEncode(req.PickupContact.Name)}</ade:Name2>
    <ade:Street>{WebUtility.HtmlEncode(street)}</ade:Street>
    <ade:CountryCode>{WebUtility.HtmlEncode(req.PickupAddress.CountryCode)}</ade:CountryCode>
    <ade:ZIPCode>{WebUtility.HtmlEncode(req.PickupAddress.PostalCode)}</ade:ZIPCode>
    <ade:City>{WebUtility.HtmlEncode(req.PickupAddress.City)}</ade:City>
    <ade:Phone>{WebUtility.HtmlEncode(req.PickupContact.Phone)}</ade:Phone>
</ade:PickupAddress>
<ade:PickupDate>{req.PickupDate:yyyy-MM-dd}</ade:PickupDate>
<ade:PickupTimeFrom>{req.PickupTimeFrom:HH:mm}</ade:PickupTimeFrom>
<ade:PickupTimeTo>{req.PickupTimeTo:HH:mm}</ade:PickupTimeTo>
<ade:ConsignIds>{consignIdsXml}</ade:ConsignIds>";
    }

    /// <summary>
    /// Mapuje dane GLS na <see cref="ShipmentResult"/>.
    /// </summary>
    /// <param name="consignmentId">Identyfikator przesyłki GLS.</param>
    /// <param name="trackingNumber">Numer śledzenia lub null.</param>
    /// <param name="labelBytes">Opcjonalne bajty etykiety PDF.</param>
    /// <returns>Wynik rejestracji przesyłki.</returns>
    public static ShipmentResult ToShipmentResult(string consignmentId, string? trackingNumber, byte[]? labelBytes)
    {
        return new ShipmentResult
        {
            TrackingNumber = trackingNumber ?? consignmentId,
            CarrierShipmentId = consignmentId,
            Label = labelBytes != null
                ? new LabelResult
                {
                    Content = labelBytes,
                    ContentType = "application/pdf",
                    FileName = $"gls_label_{consignmentId}.pdf",
                }
                : null,
        };
    }

    /// <summary>
    /// Mapuje identyfikator dyspozycji odbioru na <see cref="PickupResult"/>.
    /// </summary>
    /// <param name="confirmationId">Identyfikator potwierdzenia odbioru GLS.</param>
    /// <returns>Wynik zamówienia odbioru.</returns>
    public static PickupResult ToPickupResult(string confirmationId)
    {
        return new PickupResult
        {
            PickupOrderId = confirmationId,
        };
    }

    /// <summary>
    /// Mapuje zdarzenia śledzenia GLS na <see cref="TrackingResult"/>.
    /// </summary>
    /// <param name="trackingNumber">Numer śledzenia przesyłki.</param>
    /// <param name="events">Lista zdarzeń z GLS.</param>
    /// <returns>Wynik śledzenia przesyłki.</returns>
    public static TrackingResult ToTrackingResult(
        string trackingNumber,
        IReadOnlyList<(string status, string description, DateTimeOffset time, string? location)> events)
    {
        var trackingEvents = events
            .Select(e => new TrackingEvent
            {
                OccurredAt = e.time,
                Status = MapStatus(e.status),
                Description = e.description,
                Location = e.location,
            })
            .OrderByDescending(e => e.OccurredAt)
            .ToList();

        var latestStatus = trackingEvents.Count > 0 ? trackingEvents[0].Status : ShipmentStatus.Unknown;
        var latestDescription = trackingEvents.Count > 0 ? trackingEvents[0].Description : string.Empty;

        return new TrackingResult
        {
            TrackingNumber = trackingNumber,
            CurrentStatus = latestStatus,
            StatusDescription = latestDescription,
            Events = trackingEvents,
        };
    }

    /// <summary>
    /// Mapuje dane GLS na <see cref="DeliveryConfirmationResult"/>.
    /// </summary>
    /// <param name="trackingNumber">Numer śledzenia przesyłki.</param>
    /// <param name="deliveredAt">Data i czas doręczenia.</param>
    /// <param name="receivedBy">Imię odbiorcy lub null.</param>
    /// <returns>Wynik potwierdzenia doręczenia.</returns>
    public static DeliveryConfirmationResult ToDeliveryConfirmationResult(
        string trackingNumber,
        DateTimeOffset deliveredAt,
        string? receivedBy)
    {
        return new DeliveryConfirmationResult
        {
            TrackingNumber = trackingNumber,
            DeliveredAt = deliveredAt,
            ReceivedBy = receivedBy,
        };
    }

    /// <summary>
    /// Mapuje kod statusu GLS na znormalizowany <see cref="ShipmentStatus"/>.
    /// </summary>
    private static ShipmentStatus MapStatus(string status)
    {
        return status.ToUpperInvariant().Replace(" ", string.Empty).Replace("-", string.Empty) switch
        {
            "TRANSIT" => ShipmentStatus.InTransit,
            "INWAREHOUSE" => ShipmentStatus.InTransit,
            "INTRANSIT" => ShipmentStatus.InTransit,
            "OUTFORDELIVERY" => ShipmentStatus.OutForDelivery,
            "DELIVERED" => ShipmentStatus.Delivered,
            "NOTDELIVERED" => ShipmentStatus.DeliveryAttemptFailed,
            "DELIVERYATTEMPTFAILED" => ShipmentStatus.DeliveryAttemptFailed,
            "PICKUPED" => ShipmentStatus.PickedUp,
            "PICKUP" => ShipmentStatus.PickedUp,
            "PICKEDUP" => ShipmentStatus.PickedUp,
            "RETURNED" => ShipmentStatus.ReturnedToSender,
            "RETURNEDTOSENDER" => ShipmentStatus.ReturnedToSender,
            "CANCELLED" => ShipmentStatus.Cancelled,
            "REGISTERED" => ShipmentStatus.Registered,
            _ => ShipmentStatus.Unknown,
        };
    }

    /// <summary>
    /// Buduje pełny uliczny adres z pól adresowych (ulica + numer budynku + numer lokalu).
    /// </summary>
    private static string BuildStreet(TBJ.Integrations.Shipping.Abstractions.Models.Address address)
    {
        var street = address.Street;
        if (!string.IsNullOrWhiteSpace(address.BuildingNumber))
            street += " " + address.BuildingNumber;
        if (!string.IsNullOrWhiteSpace(address.FlatNumber))
            street += "/" + address.FlatNumber;
        return street;
    }
}
