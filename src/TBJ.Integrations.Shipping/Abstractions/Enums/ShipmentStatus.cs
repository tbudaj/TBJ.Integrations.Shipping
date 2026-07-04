namespace TBJ.Integrations.Shipping.Abstractions.Enums;

/// <summary>
/// Znormalizowany status przesyłki niezależny od konkretnego kuriera.
/// Każdy adapter mapuje natywne statusy kuriera na tę enumerację.
/// </summary>
public enum ShipmentStatus
{
    /// <summary>Status nieznany lub niemożliwy do zmapowania.</summary>
    Unknown = 0,

    /// <summary>Przesyłka zarejestrowana w systemie kuriera, oczekuje na odbiór.</summary>
    Registered = 1,

    /// <summary>Kurier odebrał przesyłkę od nadawcy.</summary>
    PickedUp = 2,

    /// <summary>Przesyłka w transporcie / w sortowni.</summary>
    InTransit = 3,

    /// <summary>Przesyłka dotarła do docelowego oddziału/depo.</summary>
    OutForDelivery = 4,

    /// <summary>Próba doręczenia nieudana — nieobecność odbiorcy itp.</summary>
    DeliveryAttemptFailed = 5,

    /// <summary>Przesyłka dostarczona do odbiorcy lub paczkomatu.</summary>
    Delivered = 6,

    /// <summary>Przesyłka zwrócona do nadawcy.</summary>
    ReturnedToSender = 7,

    /// <summary>Przesyłka utracona lub uszkodzona.</summary>
    Lost = 8,

    /// <summary>Przesyłka zatrzymana przez służby celne.</summary>
    CustomsHold = 9,

    /// <summary>Przesyłka anulowana / unieważniona.</summary>
    Cancelled = 10,
}
