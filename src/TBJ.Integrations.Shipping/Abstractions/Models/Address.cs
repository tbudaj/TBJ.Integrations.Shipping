namespace TBJ.Integrations.Shipping.Abstractions.Models;

/// <summary>
/// Adres fizyczny (nadawcy, odbiorcy lub punktu odbioru kuriera).
/// </summary>
public sealed class Address
{
    /// <summary>Imię i nazwisko lub nazwa firmy.</summary>
    public required string Name { get; init; }

    /// <summary>Ulica i numer domu/lokalu.</summary>
    public required string Street { get; init; }

    /// <summary>Numer budynku (opcjonalnie, jeśli zawarty w <see cref="Street"/>).</summary>
    public string? BuildingNumber { get; init; }

    /// <summary>Numer lokalu.</summary>
    public string? FlatNumber { get; init; }

    /// <summary>Kod pocztowy (format: XX-XXX dla Polski).</summary>
    public required string PostalCode { get; init; }

    /// <summary>Miejscowość.</summary>
    public required string City { get; init; }

    /// <summary>Kod kraju ISO 3166-1 alpha-2 (domyślnie: PL).</summary>
    public string CountryCode { get; init; } = "PL";

    /// <summary>NIP firmy (wymagany przez niektórych kurierów dla nadawców B2B).</summary>
    public string? Nip { get; init; }
}
