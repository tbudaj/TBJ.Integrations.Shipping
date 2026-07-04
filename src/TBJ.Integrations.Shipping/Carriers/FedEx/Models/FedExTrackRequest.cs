namespace TBJ.Integrations.Shipping.Carriers.FedEx.Models;

/// <summary>
/// Żądanie śledzenia przesyłek w FedEx Track API.
/// </summary>
internal sealed class FedExTrackRequest
{
    /// <summary>Lista numerów śledzenia do sprawdzenia.</summary>
    public List<FedExTrackingInfo>? TrackingInfo { get; init; }

    /// <summary>Czy pobierać szczegółowe skany zdarzeń.</summary>
    public bool IncludeDetailedScans { get; init; }
}

/// <summary>
/// Informacje o numerze śledzenia FedEx.
/// </summary>
internal sealed class FedExTrackingInfo
{
    /// <summary>Informacje o numerze śledzenia.</summary>
    public FedExTrackingNumberInfo? TrackingNumberInfo { get; init; }
}

/// <summary>
/// Numer śledzenia FedEx.
/// </summary>
internal sealed class FedExTrackingNumberInfo
{
    /// <summary>Numer śledzenia przesyłki.</summary>
    public string? TrackingNumber { get; init; }
}
