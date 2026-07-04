namespace TBJ.Integrations.Shipping.Abstractions.Models;

/// <summary>
/// Informacje o pobraniu (Cash on Delivery — COD).
/// </summary>
public sealed class CodInfo
{
    /// <summary>Kwota pobrania w złotych polskich (PLN).</summary>
    public required decimal AmountPln { get; init; }

    /// <summary>
    /// Numer konta bankowego (IBAN) do przekazania pobranej kwoty.
    /// Format: PL + 26 cyfr, np. PL61109010140000071219812874.
    /// </summary>
    public required string BankAccountIban { get; init; }

    /// <summary>Referencja do przelewu (opcjonalna).</summary>
    public string? Reference { get; init; }
}
