using System.Globalization;
using System.Net;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.DPD.Configuration;

namespace TBJ.Integrations.Shipping.Carriers.DPD.Mappers;

/// <summary>
/// Mapuje domenowe modele wysyłkowe na XML DPD oraz odpowiedzi XML na modele wynikowe.
/// </summary>
internal static class DpdMapper
{
    /// <summary>
    /// Buduje element &lt;openUMLF&gt; wymagany przez operację <c>generatePackagesNumbersV1</c>.
    /// </summary>
    /// <param name="req">Żądanie rejestracji przesyłki.</param>
    /// <returns>Fragment XML.</returns>
    public static string BuildOpenUmlf(CreateShipmentRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(req.SenderAddress);
        ArgumentNullException.ThrowIfNull(req.RecipientAddress);
        ArgumentNullException.ThrowIfNull(req.Parcels);

        var senderStreet = BuildStreet(req.SenderAddress);
        var recipientStreet = BuildStreet(req.RecipientAddress);

        var parcelsXml = string.Join(
            string.Empty,
            req.Parcels.Select((parcel, index) => $@"
            <ns:parcel>
                <ns:content>{WebUtility.HtmlEncode(req.Reference ?? $"Paczka {index + 1}")}</ns:content>
                <ns:weight>{parcel.WeightKg.ToString(CultureInfo.InvariantCulture)}</ns:weight>
                {(parcel.LengthCm.HasValue ? $"<ns:length>{parcel.LengthCm.Value.ToString(CultureInfo.InvariantCulture)}</ns:length>" : string.Empty)}
                {(parcel.WidthCm.HasValue ? $"<ns:width>{parcel.WidthCm.Value.ToString(CultureInfo.InvariantCulture)}</ns:width>" : string.Empty)}
                {(parcel.HeightCm.HasValue ? $"<ns:height>{parcel.HeightCm.Value.ToString(CultureInfo.InvariantCulture)}</ns:height>" : string.Empty)}
            </ns:parcel>"));

        var codXml = req.Cod != null
            ? $@"
            <ns:cod>
                <ns:amount>{req.Cod.AmountPln.ToString(CultureInfo.InvariantCulture)}</ns:amount>
                <ns:reference>{WebUtility.HtmlEncode(req.Cod.Reference ?? req.Reference ?? string.Empty)}</ns:reference>
            </ns:cod>"
            : string.Empty;

        var insuranceXml = req.Insurance != null
            ? $@"
            <ns:insurance>
                <ns:amount>{req.Insurance.DeclaredValuePln.ToString(CultureInfo.InvariantCulture)}</ns:amount>
            </ns:insurance>"
            : string.Empty;

        return $@"
<ns:openUMLF>
    <ns:packages>
        <ns:sender>
            <ns:company>{WebUtility.HtmlEncode(req.SenderAddress.Name)}</ns:company>
            <ns:name>{WebUtility.HtmlEncode(req.SenderContact.Name)}</ns:name>
            <ns:address>{WebUtility.HtmlEncode(senderStreet)}</ns:address>
            <ns:city>{WebUtility.HtmlEncode(req.SenderAddress.City)}</ns:city>
            <ns:postalCode>{WebUtility.HtmlEncode(req.SenderAddress.PostalCode)}</ns:postalCode>
            <ns:countryCode>{WebUtility.HtmlEncode(req.SenderAddress.CountryCode)}</ns:countryCode>
            <ns:phone>{WebUtility.HtmlEncode(req.SenderContact.Phone)}</ns:phone>
            <ns:email>{WebUtility.HtmlEncode(req.SenderContact.Email ?? string.Empty)}</ns:email>
            <ns:nip>{WebUtility.HtmlEncode(req.SenderAddress.Nip ?? string.Empty)}</ns:nip>
        </ns:sender>
        <ns:receiver>
            <ns:company>{WebUtility.HtmlEncode(req.RecipientAddress.Name)}</ns:company>
            <ns:name>{WebUtility.HtmlEncode(req.RecipientContact.Name)}</ns:name>
            <ns:address>{WebUtility.HtmlEncode(recipientStreet)}</ns:address>
            <ns:city>{WebUtility.HtmlEncode(req.RecipientAddress.City)}</ns:city>
            <ns:postalCode>{WebUtility.HtmlEncode(req.RecipientAddress.PostalCode)}</ns:postalCode>
            <ns:countryCode>{WebUtility.HtmlEncode(req.RecipientAddress.CountryCode)}</ns:countryCode>
            <ns:phone>{WebUtility.HtmlEncode(req.RecipientContact.Phone)}</ns:phone>
            <ns:email>{WebUtility.HtmlEncode(req.RecipientContact.Email ?? string.Empty)}</ns:email>
            <ns:nip>{WebUtility.HtmlEncode(req.RecipientAddress.Nip ?? string.Empty)}</ns:nip>
        </ns:receiver>
        <ns:parcels>{parcelsXml}</ns:parcels>
        {codXml}
        {insuranceXml}
        <ns:reference>{WebUtility.HtmlEncode(req.Reference ?? string.Empty)}</ns:reference>
        <ns:comment>{WebUtility.HtmlEncode(req.Comment ?? string.Empty)}</ns:comment>
    </ns:packages>
    <ns:pkgNumsGenerationMode>EACH_PACKAGE</ns:pkgNumsGenerationMode>
</ns:openUMLF>";
    }

    /// <summary>
    /// Buduje XML dla operacji <c>packagesPickupCallV1</c>.
    /// </summary>
    /// <param name="req">Żądanie zamówienia odbioru.</param>
    /// <param name="waybills">Lista numerów listów przewozowych do odebrania.</param>
    /// <param name="opts">Opcje DPD.</param>
    /// <returns>Fragment XML.</returns>
    public static string BuildPickupXml(OrderPickupRequest req, IReadOnlyList<string> waybills, DpdOptions opts)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(req.PickupAddress);
        ArgumentNullException.ThrowIfNull(waybills);

        var waybillsXml = string.Join(
            string.Empty,
            waybills.Select(w => $"<ns:waybills>{WebUtility.HtmlEncode(w)}</ns:waybills>"));

        var street = BuildStreet(req.PickupAddress);

        return $@"
<ns:pickupDate>{req.PickupDate:yyyy-MM-dd}</ns:pickupDate>
<ns:pickupTimeFrom>{req.PickupTimeFrom:HH:mm}</ns:pickupTimeFrom>
<ns:pickupTimeTo>{req.PickupTimeTo:HH:mm}</ns:pickupTimeTo>
<ns:ordererPhone>{WebUtility.HtmlEncode(req.PickupContact.Phone)}</ns:ordererPhone>
<ns:pickupAddress>
    <ns:company>{WebUtility.HtmlEncode(req.PickupAddress.Name)}</ns:company>
    <ns:address>{WebUtility.HtmlEncode(street)}</ns:address>
    <ns:city>{WebUtility.HtmlEncode(req.PickupAddress.City)}</ns:city>
    <ns:postalCode>{WebUtility.HtmlEncode(req.PickupAddress.PostalCode)}</ns:postalCode>
    <ns:countryCode>{WebUtility.HtmlEncode(req.PickupAddress.CountryCode)}</ns:countryCode>
    <ns:phone>{WebUtility.HtmlEncode(req.PickupContact.Phone)}</ns:phone>
    <ns:nip>{WebUtility.HtmlEncode(req.PickupAddress.Nip ?? string.Empty)}</ns:nip>
</ns:pickupAddress>
<ns:waybills>{waybillsXml}</ns:waybills>";
    }

    /// <summary>
    /// Mapuje wynik rejestracji paczki na <see cref="ShipmentResult"/>.
    /// </summary>
    /// <param name="packageId">Identyfikator paczki DPD.</param>
    /// <param name="waybill">Numer listu przewozowego.</param>
    /// <param name="labelBytes">Opcjonalne bajty etykiety.</param>
    /// <returns>Wynik rejestracji.</returns>
    public static ShipmentResult ToShipmentResult(string packageId, string waybill, byte[]? labelBytes)
    {
        return new ShipmentResult
        {
            TrackingNumber = waybill,
            CarrierShipmentId = packageId,
            Label = labelBytes != null ? new LabelResult
            {
                Content = labelBytes,
                ContentType = "application/pdf",
                FileName = $"dpd_label_{waybill}.pdf",
            } : null,
        };
    }

    /// <summary>
    /// Mapuje identyfikator dyspozycji odbioru na <see cref="PickupResult"/>.
    /// </summary>
    /// <param name="documentId">Identyfikator dokumentu odbioru.</param>
    /// <returns>Wynik zamówienia odbioru.</returns>
    public static PickupResult ToPickupResult(string documentId)
    {
        return new PickupResult
        {
            PickupOrderId = documentId,
        };
    }

    /// <summary>
    /// Mapuje status śledzenia DPD na <see cref="TrackingResult"/>.
    /// </summary>
    /// <param name="trackingNumber">Numer śledzenia.</param>
    /// <param name="state">Kod statusu DPD.</param>
    /// <param name="description">Opis statusu.</param>
    /// <param name="eventTime">Czas zdarzenia.</param>
    /// <returns>Wynik śledzenia.</returns>
    public static TrackingResult ToTrackingResult(
        string trackingNumber,
        string state,
        string description,
        DateTime? eventTime)
    {
        var status = MapStatus(state);
        var occurredAt = eventTime.HasValue
            ? new DateTimeOffset(eventTime.Value, TimeSpan.Zero)
            : DateTimeOffset.UtcNow;

        return new TrackingResult
        {
            TrackingNumber = trackingNumber,
            CurrentStatus = status,
            StatusDescription = description,
            Events = new List<TrackingEvent>
            {
                new TrackingEvent
                {
                    OccurredAt = occurredAt,
                    Status = status,
                    Description = description,
                },
            },
        };
    }

    /// <summary>
    /// Mapuje potwierdzenie doręczenia DPD na <see cref="DeliveryConfirmationResult"/>.
    /// </summary>
    /// <param name="trackingNumber">Numer śledzenia.</param>
    /// <param name="description">Opis doręczenia.</param>
    /// <param name="eventTime">Czas doręczenia.</param>
    /// <returns>Wynik potwierdzenia doręczenia.</returns>
    public static DeliveryConfirmationResult ToDeliveryConfirmationResult(
        string trackingNumber,
        string description,
        DateTime eventTime)
    {
        return new DeliveryConfirmationResult
        {
            TrackingNumber = trackingNumber,
            DeliveredAt = new DateTimeOffset(eventTime, TimeSpan.Zero),
            ReceivedBy = description,
        };
    }

    private static string BuildStreet(Address address)
    {
        var street = address.Street;
        if (!string.IsNullOrWhiteSpace(address.BuildingNumber))
            street += " " + address.BuildingNumber;
        if (!string.IsNullOrWhiteSpace(address.FlatNumber))
            street += "/" + address.FlatNumber;
        return street;
    }

    private static ShipmentStatus MapStatus(string state)
    {
        return state?.ToUpperInvariant() switch
        {
            "P" => ShipmentStatus.InTransit,
            "D" => ShipmentStatus.Delivered,
            "B" => ShipmentStatus.ReturnedToSender,
            "T" => ShipmentStatus.InTransit,
            "C" => ShipmentStatus.PickedUp,
            _ => ShipmentStatus.Unknown,
        };
    }
}
