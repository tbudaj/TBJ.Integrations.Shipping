namespace TBJ.Integrations.Shipping.Carriers.FedEx.Models;

/// <summary>
/// Odpowiedź FedEx API przy zamówieniu odbioru przesyłek.
/// </summary>
internal sealed class FedExPickupResponse
{
    /// <summary>Dane wyjściowe zamówienia odbioru.</summary>
    public FedExPickupOutput? Output { get; init; }
}

/// <summary>
/// Dane wyjściowe odpowiedzi zamówienia odbioru FedEx.
/// </summary>
internal sealed class FedExPickupOutput
{
    /// <summary>Kod potwierdzenia zamówienia odbioru.</summary>
    public string? PickupConfirmationCode { get; init; }

    /// <summary>Lokalizacja FedEx odpowiedzialna za odbiór.</summary>
    public string? Location { get; init; }
}
