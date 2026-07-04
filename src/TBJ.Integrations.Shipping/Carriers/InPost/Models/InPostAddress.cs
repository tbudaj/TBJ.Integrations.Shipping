namespace TBJ.Integrations.Shipping.Carriers.InPost.Models;

/// <summary>
/// Model adresowy zgodny ze specyfikacją InPost ShipX API.
/// </summary>
internal sealed class InPostAddress
{
    /// <summary>Imię kontaktowe.</summary>
    public string? FirstName { get; set; }

    /// <summary>Nazwisko kontaktowe.</summary>
    public string? LastName { get; set; }

    /// <summary>Nazwa firmy (opcjonalnie).</summary>
    public string? CompanyName { get; set; }

    /// <summary>Adres e-mail.</summary>
    public string? Email { get; set; }

    /// <summary>Numer telefonu.</summary>
    public string? Phone { get; set; }

    /// <summary>Zagnieżdżony obiekt adresu fizycznego.</summary>
    public InPostAddressDetails? Address { get; set; }
}

/// <summary>
/// Szczegóły adresu fizycznego w formacie InPost ShipX API.
/// </summary>
internal sealed class InPostAddressDetails
{
    /// <summary>Ulica.</summary>
    public string? Street { get; set; }

    /// <summary>Numer budynku.</summary>
    public string? BuildingNumber { get; set; }

    /// <summary>Miasto.</summary>
    public string? City { get; set; }

    /// <summary>Kod pocztowy.</summary>
    public string? PostCode { get; set; }

    /// <summary>Kod kraju ISO 3166-1 alpha-2.</summary>
    public string? CountryCode { get; set; }
}
