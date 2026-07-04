namespace TBJ.Integrations.Shipping.Abstractions.Models;

/// <summary>
/// Wymiary i waga przesyłki/paczki.
/// </summary>
public sealed class ParcelDimensions
{
    /// <summary>Waga w kilogramach.</summary>
    public required decimal WeightKg { get; init; }

    /// <summary>Długość w centymetrach (opcjonalna — nie wszystkie kurierzy wymagają).</summary>
    public decimal? LengthCm { get; init; }

    /// <summary>Szerokość w centymetrach.</summary>
    public decimal? WidthCm { get; init; }

    /// <summary>Wysokość w centymetrach.</summary>
    public decimal? HeightCm { get; init; }
}
