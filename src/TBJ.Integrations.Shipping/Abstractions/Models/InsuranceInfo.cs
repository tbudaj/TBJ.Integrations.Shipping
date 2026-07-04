namespace TBJ.Integrations.Shipping.Abstractions.Models;

/// <summary>
/// Informacje o ubezpieczeniu przesyłki.
/// </summary>
public sealed class InsuranceInfo
{
    /// <summary>Zadeklarowana wartość przesyłki w złotych polskich (PLN).</summary>
    public required decimal DeclaredValuePln { get; init; }
}
