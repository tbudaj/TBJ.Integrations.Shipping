using System.Globalization;
using System.Net;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.DHL.Configuration;

namespace TBJ.Integrations.Shipping.Carriers.DHL.Mappers;

/// <summary>
/// Mapuje domenowe modele wysyłkowe na XML DHL oraz odpowiedzi XML na modele wynikowe.
/// </summary>
internal static class DhlMapper
{
    /// <summary>
    /// Buduje XML przesyłek dla operacji <c>createShipments</c>.
    /// </summary>
    /// <param name="req">Żądanie rejestracji przesyłki.</param>
    /// <param name="opts">Opcje DHL.</param>
    /// <returns>Fragment XML.</returns>
    public static string BuildShipmentXml(CreateShipmentRequest req, DhlOptions opts)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(req.SenderAddress);
        ArgumentNullException.ThrowIfNull(req.RecipientAddress);
        ArgumentNullException.ThrowIfNull(req.Parcels);

        var serviceType = string.IsNullOrWhiteSpace(req.ServiceCode) ? opts.DefaultServiceType : req.ServiceCode;

        var piecesXml = string.Join(
            string.Empty,
            req.Parcels.Select((parcel, index) => $@"
            <ns:piece>
                <ns:type>PACKAGE</ns:type>
                <ns:weight>{parcel.WeightKg.ToString(CultureInfo.InvariantCulture)}</ns:weight>
                {(parcel.LengthCm.HasValue ? $"<ns:length>{parcel.LengthCm.Value.ToString(CultureInfo.InvariantCulture)}</ns:length>" : string.Empty)}
                {(parcel.WidthCm.HasValue ? $"<ns:width>{parcel.WidthCm.Value.ToString(CultureInfo.InvariantCulture)}</ns:width>" : string.Empty)}
                {(parcel.HeightCm.HasValue ? $"<ns:height>{parcel.HeightCm.Value.ToString(CultureInfo.InvariantCulture)}</ns:height>" : string.Empty)}
            </ns:piece>"));

        var codXml = req.Cod != null
            ? $@"
                <ns:service>
                    <ns:serviceType>COD</ns:serviceType>
                    <ns:serviceValue>{req.Cod.AmountPln.ToString(CultureInfo.InvariantCulture)}</ns:serviceValue>
                    <ns:bankAccountNumber>{WebUtility.HtmlEncode(req.Cod.BankAccountIban)}</ns:bankAccountNumber>
                </ns:service>"
            : string.Empty;

        var insuranceXml = req.Insurance != null
            ? $@"
                <ns:service>
                    <ns:serviceType>INSURANCE</ns:serviceType>
                    <ns:serviceValue>{req.Insurance.DeclaredValuePln.ToString(CultureInfo.InvariantCulture)}</ns:serviceValue>
                </ns:service>"
            : string.Empty;

        var specialServicesXml = !string.IsNullOrEmpty(codXml + insuranceXml)
            ? $"<ns:specialServices>{codXml}{insuranceXml}</ns:specialServices>"
            : string.Empty;

        return $@"
<ns:shipments>
    <ns:item>
        <ns:shipper>
            {BuildAddressXml(req.SenderAddress, req.SenderContact)}
        </ns:shipper>
        <ns:receiver>
            {BuildAddressXml(req.RecipientAddress, req.RecipientContact)}
        </ns:receiver>
        <ns:pieceList>{piecesXml}</ns:pieceList>
        <ns:serviceType>{WebUtility.HtmlEncode(serviceType)}</ns:serviceType>
        {specialServicesXml}
        <ns:date>{DateOnly.FromDateTime(DateTime.Today):yyyy-MM-dd}</ns:date>
        <ns:content>{WebUtility.HtmlEncode(req.Reference ?? "Przesyłka")}</ns:content>
        <ns:comment>{WebUtility.HtmlEncode(req.Comment ?? string.Empty)}</ns:comment>
        <ns:reference>{WebUtility.HtmlEncode(req.Reference ?? string.Empty)}</ns:reference>
    </ns:item>
</ns:shipments>";
    }

    /// <summary>
    /// Buduje XML dla operacji <c>bookCourier</c>.
    /// </summary>
    /// <param name="req">Żądanie zamówienia kuriera.</param>
    /// <param name="shipmentIds">Lista identyfikatorów przesyłek.</param>
    /// <returns>Fragment XML.</returns>
    public static string BuildBookCourierXml(OrderPickupRequest req, IReadOnlyList<string> shipmentIds)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(req.PickupAddress);
        ArgumentNullException.ThrowIfNull(shipmentIds);

        var shipmentsXml = string.Join(
            string.Empty,
            shipmentIds.Select(id => $"<ns:item>{WebUtility.HtmlEncode(id)}</ns:item>"));

        return $@"
<ns:shipmentTime>
    <ns:date>{req.PickupDate:yyyy-MM-dd}</ns:date>
    <ns:from>{req.PickupTimeFrom:HH:mm}</ns:from>
    <ns:to>{req.PickupTimeTo:HH:mm}</ns:to>
</ns:shipmentTime>
<ns:pickupAddress>
    {BuildAddressXml(req.PickupAddress, req.PickupContact)}
</ns:pickupAddress>
<ns:shipments>{shipmentsXml}</ns:shipments>";
    }

    /// <summary>
    /// Mapuje wynik rejestracji przesyłki na <see cref="ShipmentResult"/>.
    /// </summary>
    /// <param name="shipmentId">Identyfikator przesyłki DHL.</param>
    /// <param name="labelId">Identyfikator etykiety.</param>
    /// <param name="labelBytes">Opcjonalne bajty etykiety.</param>
    /// <returns>Wynik rejestracji.</returns>
    public static ShipmentResult ToShipmentResult(string shipmentId, string? labelId, byte[]? labelBytes)
    {
        return new ShipmentResult
        {
            TrackingNumber = shipmentId,
            CarrierShipmentId = shipmentId,
            Label = labelBytes != null ? new LabelResult
            {
                Content = labelBytes,
                ContentType = "application/pdf",
                FileName = $"dhl_label_{labelId ?? shipmentId}.pdf",
            } : null,
        };
    }

    /// <summary>
    /// Mapuje potwierdzenie rezerwacji kuriera na <see cref="PickupResult"/>.
    /// </summary>
    /// <param name="confirmationId">Identyfikator potwierdzenia.</param>
    /// <returns>Wynik zamówienia odbioru.</returns>
    public static PickupResult ToPickupResult(string confirmationId)
    {
        return new PickupResult
        {
            PickupOrderId = confirmationId,
        };
    }

    /// <summary>
    /// Mapuje historię zdarzeń śledzenia DHL na <see cref="TrackingResult"/>.
    /// </summary>
    /// <param name="trackingNumber">Numer śledzenia.</param>
    /// <param name="events">Lista zdarzeń z API DHL.</param>
    /// <returns>Wynik śledzenia.</returns>
    public static TrackingResult ToTrackingResult(
        string trackingNumber,
        IReadOnlyList<(string status, string desc, DateTimeOffset time, string? loc)> events)
    {
        var orderedEvents = events
            .Select(e => new TrackingEvent
            {
                OccurredAt = e.time,
                Status = MapStatus(e.status),
                Description = e.desc,
                Location = e.loc,
            })
            .OrderByDescending(e => e.OccurredAt)
            .ToList();

        var latest = orderedEvents.FirstOrDefault();

        return new TrackingResult
        {
            TrackingNumber = trackingNumber,
            CurrentStatus = latest?.Status ?? ShipmentStatus.Unknown,
            StatusDescription = latest?.Description ?? string.Empty,
            Events = orderedEvents,
        };
    }

    /// <summary>
    /// Mapuje potwierdzenie doręczenia DHL na <see cref="DeliveryConfirmationResult"/>.
    /// </summary>
    /// <param name="trackingNumber">Numer śledzenia.</param>
    /// <param name="receivedBy">Osoba odbierająca.</param>
    /// <param name="deliveredAt">Czas doręczenia.</param>
    /// <param name="podBytes">Opcjonalne bajty dokumentu POD.</param>
    /// <returns>Wynik potwierdzenia doręczenia.</returns>
    public static DeliveryConfirmationResult ToDeliveryConfirmationResult(
        string trackingNumber,
        string receivedBy,
        DateTimeOffset deliveredAt,
        byte[]? podBytes)
    {
        return new DeliveryConfirmationResult
        {
            TrackingNumber = trackingNumber,
            DeliveredAt = deliveredAt,
            ReceivedBy = receivedBy,
            PodDocument = podBytes,
            PodContentType = podBytes != null ? "application/pdf" : null,
        };
    }

    private static string BuildAddressXml(Address address, ContactInfo contact)
    {
        var street = address.Street;
        if (!string.IsNullOrWhiteSpace(address.BuildingNumber))
            street += " " + address.BuildingNumber;
        if (!string.IsNullOrWhiteSpace(address.FlatNumber))
            street += "/" + address.FlatNumber;

        return $@"
<ns:name>{WebUtility.HtmlEncode(address.Name)}</ns:name>
<ns:company>{WebUtility.HtmlEncode(contact.CompanyName ?? address.Name)}</ns:company>
<ns:street>{WebUtility.HtmlEncode(street)}</ns:street>
<ns:postalCode>{WebUtility.HtmlEncode(address.PostalCode)}</ns:postalCode>
<ns:city>{WebUtility.HtmlEncode(address.City)}</ns:city>
<ns:country>{WebUtility.HtmlEncode(address.CountryCode)}</ns:country>
<ns:phone>{WebUtility.HtmlEncode(contact.Phone)}</ns:phone>
<ns:email>{WebUtility.HtmlEncode(contact.Email ?? string.Empty)}</ns:email>
<ns:nip>{WebUtility.HtmlEncode(address.Nip ?? string.Empty)}</ns:nip>";
    }

    private static ShipmentStatus MapStatus(string status)
    {
        return status?.ToLowerInvariant() switch
        {
            "delivered" => ShipmentStatus.Delivered,
            "in transit" => ShipmentStatus.InTransit,
            "out for delivery" => ShipmentStatus.OutForDelivery,
            "picked up" => ShipmentStatus.PickedUp,
            "attempt failed" => ShipmentStatus.DeliveryAttemptFailed,
            "returned" => ShipmentStatus.ReturnedToSender,
            _ => ShipmentStatus.Unknown,
        };
    }
}
