using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Models;
using TBJ.Integrations.Shipping.Carriers.InPost.Models;

namespace TBJ.Integrations.Shipping.Carriers.InPost.Mappers;

/// <summary>
/// Mapper statyczny konwertujący modele abstrakcyjne na modele InPost ShipX API i odwrotnie.
/// </summary>
internal static class InPostMapper
{
    /// <summary>
    /// Konwertuje żądanie rejestracji przesyłki na model InPost ShipX API.
    /// </summary>
    /// <param name="req">Żądanie rejestracji z modeli abstrakcyjnych.</param>
    /// <returns>Żądanie w formacie InPost ShipX API.</returns>
    public static InPostCreateShipmentRequest ToInPostRequest(CreateShipmentRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        var result = new InPostCreateShipmentRequest
        {
            Receiver = MapToInPostAddress(req.RecipientAddress, req.RecipientContact),
            Sender = MapToInPostAddress(req.SenderAddress, req.SenderContact),
            Parcels = req.Parcels.Select(MapToInPostParcel).ToList(),
            Service = req.ServiceCode,
            Reference = req.Reference,
            Comments = req.Comment,
        };

        if (req.Cod is not null)
        {
            result.Cod = new InPostCod
            {
                Amount = req.Cod.AmountPln,
                Currency = "PLN",
            };
        }

        if (req.Insurance is not null)
        {
            result.Insurance = new InPostInsurance
            {
                Amount = req.Insurance.DeclaredValuePln,
                Currency = "PLN",
            };
        }

        if (!string.IsNullOrWhiteSpace(req.LockerTargetMachineId))
        {
            result.CustomAttributes = new Dictionary<string, string>
            {
                ["target_machine_id"] = req.LockerTargetMachineId,
            };
        }

        return result;
    }

    /// <summary>
    /// Konwertuje odpowiedź InPost na wynik rejestracji przesyłki.
    /// </summary>
    /// <param name="resp">Odpowiedź InPost ShipX API.</param>
    /// <param name="label">Opcjonalny wynik etykiety.</param>
    /// <returns>Wynik rejestracji przesyłki.</returns>
    public static ShipmentResult ToShipmentResult(InPostShipmentResponse resp, LabelResult? label)
    {
        ArgumentNullException.ThrowIfNull(resp);

        return new ShipmentResult
        {
            TrackingNumber = resp.TrackingNumber ?? string.Empty,
            CarrierShipmentId = resp.Id.ToString(),
            Label = label,
            RawCarrierResponse = null,
        };
    }

    /// <summary>
    /// Konwertuje żądanie odbioru na model zlecenia odbioru InPost (dispatch order).
    /// </summary>
    /// <param name="req">Żądanie odbioru z modeli abstrakcyjnych.</param>
    /// <param name="shipmentIds">Lista identyfikatorów przesyłek InPost.</param>
    /// <returns>Żądanie zlecenia odbioru w formacie InPost ShipX API.</returns>
    public static InPostDispatchOrderRequest ToDispatchOrderRequest(
        OrderPickupRequest req,
        IReadOnlyList<string> shipmentIds)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(shipmentIds);

        return new InPostDispatchOrderRequest
        {
            Shipments = shipmentIds.ToList(),
            Address = MapToInPostAddress(req.PickupAddress, req.PickupContact),
        };
    }

    /// <summary>
    /// Konwertuje odpowiedź zlecenia odbioru InPost na wynik odbioru.
    /// </summary>
    /// <param name="resp">Odpowiedź InPost ShipX API.</param>
    /// <returns>Wynik dyspozycji odbioru.</returns>
    public static PickupResult ToPickupResult(InPostDispatchOrderResponse resp)
    {
        ArgumentNullException.ThrowIfNull(resp);

        return new PickupResult
        {
            PickupOrderId = resp.Id.ToString(),
            RawCarrierResponse = null,
        };
    }

    /// <summary>
    /// Konwertuje odpowiedź śledzenia InPost na znormalizowany wynik śledzenia.
    /// </summary>
    /// <param name="resp">Odpowiedź śledzenia InPost ShipX API.</param>
    /// <returns>Znormalizowany wynik śledzenia.</returns>
    public static TrackingResult ToTrackingResult(InPostTrackingResponse resp)
    {
        ArgumentNullException.ThrowIfNull(resp);

        var events = resp.Events?
            .Select(e => new TrackingEvent
            {
                OccurredAt = e.OccurredAt ?? DateTimeOffset.MinValue,
                Status = MapInPostStatus(e.Status),
                Description = e.Description ?? string.Empty,
            })
            .OrderByDescending(e => e.OccurredAt)
            .ToList()
            ?? [];

        return new TrackingResult
        {
            TrackingNumber = resp.TrackingNumber ?? string.Empty,
            CurrentStatus = MapInPostStatus(resp.Status),
            StatusDescription = resp.Status ?? string.Empty,
            Events = events,
            RawCarrierResponse = null,
        };
    }

    /// <summary>
    /// Konwertuje odpowiedź śledzenia InPost na wynik potwierdzenia doręczenia.
    /// </summary>
    /// <param name="resp">Odpowiedź śledzenia InPost ShipX API.</param>
    /// <returns>Wynik potwierdzenia doręczenia.</returns>
    public static DeliveryConfirmationResult ToDeliveryConfirmationResult(InPostTrackingResponse resp)
    {
        ArgumentNullException.ThrowIfNull(resp);

        var deliveredEvent = resp.Events?
            .FirstOrDefault(e => string.Equals(e.Status, "delivered", StringComparison.OrdinalIgnoreCase));

        return new DeliveryConfirmationResult
        {
            TrackingNumber = resp.TrackingNumber ?? string.Empty,
            DeliveredAt = deliveredEvent?.OccurredAt ?? resp.UpdatedAt ?? DateTimeOffset.MinValue,
            ReceivedBy = null,
            RawCarrierResponse = null,
        };
    }

    /// <summary>
    /// Mapuje natywny status InPost na znormalizowany <see cref="ShipmentStatus"/>.
    /// </summary>
    /// <param name="inPostStatus">Status zwrócony przez InPost ShipX API.</param>
    /// <returns>Znormalizowany status przesyłki.</returns>
    public static ShipmentStatus MapInPostStatus(string? inPostStatus)
    {
        if (string.IsNullOrWhiteSpace(inPostStatus))
            return ShipmentStatus.Unknown;

        return inPostStatus.ToLowerInvariant() switch
        {
            "created" => ShipmentStatus.Registered,
            "confirmed" => ShipmentStatus.Registered,
            "dispatched_by_sender" => ShipmentStatus.PickedUp,
            "collected_from_sender" => ShipmentStatus.PickedUp,
            "taken_by_courier" => ShipmentStatus.PickedUp,
            "adopted_at_source_branch" => ShipmentStatus.InTransit,
            "sent_from_source_branch" => ShipmentStatus.InTransit,
            "adopted_at_sorting_center" => ShipmentStatus.InTransit,
            "sent_from_sorting_center" => ShipmentStatus.InTransit,
            "adopted_at_target_branch" => ShipmentStatus.OutForDelivery,
            "out_for_delivery" => ShipmentStatus.OutForDelivery,
            "ready_to_pickup" => ShipmentStatus.OutForDelivery,
            "pickup_reminder_sent" => ShipmentStatus.OutForDelivery,
            "delivered" => ShipmentStatus.Delivered,
            "claimed" => ShipmentStatus.Delivered,
            "undelivered" => ShipmentStatus.DeliveryAttemptFailed,
            "avizo" => ShipmentStatus.DeliveryAttemptFailed,
            "returned_to_sender" => ShipmentStatus.ReturnedToSender,
            "cancelled" => ShipmentStatus.Cancelled,
            _ => ShipmentStatus.Unknown,
        };
    }

    /// <summary>
    /// Mapuje adres i dane kontaktowe na model InPost.
    /// </summary>
    private static InPostAddress MapToInPostAddress(Address address, ContactInfo contact)
    {
        // Próba rozdzielenia Name na imię i nazwisko
        var nameParts = contact.Name?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new InPostAddress
        {
            FirstName = nameParts is { Length: > 0 } ? nameParts[0] : contact.Name,
            LastName = nameParts is { Length: > 1 } ? nameParts[1] : null,
            CompanyName = contact.CompanyName,
            Email = contact.Email,
            Phone = contact.Phone,
            Address = new InPostAddressDetails
            {
                Street = address.Street,
                BuildingNumber = address.BuildingNumber,
                City = address.City,
                PostCode = address.PostalCode,
                CountryCode = address.CountryCode,
            },
        };
    }

    /// <summary>
    /// Konwertuje wymiary paczki (cm → mm, kg → g) na model InPost.
    /// </summary>
    private static InPostParcel MapToInPostParcel(ParcelDimensions parcel)
    {
        return new InPostParcel
        {
            Dimensions = new InPostDimensions
            {
                Length = (parcel.LengthCm ?? 0) * 10m,
                Width = (parcel.WidthCm ?? 0) * 10m,
                Height = (parcel.HeightCm ?? 0) * 10m,
            },
            Weight = new InPostWeight
            {
                Amount = parcel.WeightKg * 1000m,
                Unit = "g",
            },
            IsNonStandard = false,
        };
    }
}
