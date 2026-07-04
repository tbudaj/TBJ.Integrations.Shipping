namespace TBJ.Integrations.Shipping.Abstractions.Enums;

/// <summary>
/// Identyfikator firmy kurierskiej obsługiwanej przez Shipping Gateway.
/// Wartość przekazywana jako parametr do metod <see cref="Interfaces.IShippingGateway"/>
/// w celu wybrania konkretnego adaptera kurierskiego.
/// </summary>
public enum CarrierType
{
    /// <summary>InPost — dostawa do Paczkomatu (ShipX API, usługi <c>inpost_locker_*</c>).</summary>
    InPostLocker = 1,

    /// <summary>InPost — dostawa kurierem door-to-door (ShipX API, usługi <c>inpost_courier_*</c>).</summary>
    InPostCourier = 2,

    /// <summary>DPD Polska (DPD Web Service SOAP).</summary>
    DPD = 3,

    /// <summary>DHL Parcel Polska / DHL24 (SOAP WebAPI2).</summary>
    DHL = 4,

    /// <summary>GLS Poland — ADE-Plus SOAP.</summary>
    GLS = 5,

    /// <summary>UPS — REST API z OAuth 2.0.</summary>
    UPS = 6,

    /// <summary>FedEx (dawne TNT) — REST API z OAuth 2.0.</summary>
    FedEx = 7,
}
