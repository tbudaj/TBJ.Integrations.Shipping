namespace TBJ.Integrations.Shipping.Carriers.InPost.Models;

/// <summary>
/// Model paczki zgodny ze specyfikacją InPost ShipX API.
/// Wymiary podawane w milimetrach, waga w gramach.
/// </summary>
internal sealed class InPostParcel
{
    /// <summary>Wymiary paczki (w milimetrach).</summary>
    public InPostDimensions? Dimensions { get; set; }

    /// <summary>Waga paczki w gramach.</summary>
    public InPostWeight? Weight { get; set; }

    /// <summary>Czy paczka jest niestandardowa (przekracza standardowe wymiary).</summary>
    public bool IsNonStandard { get; set; }
}

/// <summary>
/// Wymiary paczki w milimetrach dla InPost ShipX API.
/// </summary>
internal sealed class InPostDimensions
{
    /// <summary>Długość w milimetrach.</summary>
    public decimal Length { get; set; }

    /// <summary>Szerokość w milimetrach.</summary>
    public decimal Width { get; set; }

    /// <summary>Wysokość w milimetrach.</summary>
    public decimal Height { get; set; }
}

/// <summary>
/// Waga paczki w gramach dla InPost ShipX API.
/// </summary>
internal sealed class InPostWeight
{
    /// <summary>Waga w gramach.</summary>
    public decimal Amount { get; set; }

    /// <summary>Jednostka wagi (domyślnie: g).</summary>
    public string Unit { get; set; } = "g";
}
