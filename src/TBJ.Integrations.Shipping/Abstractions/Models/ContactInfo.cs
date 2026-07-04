namespace TBJ.Integrations.Shipping.Abstractions.Models;

/// <summary>
/// Dane kontaktowe osoby lub firmy (nadawcy bądź odbiorcy przesyłki).
/// </summary>
public sealed class ContactInfo
{
    /// <summary>Imię i nazwisko osoby kontaktowej.</summary>
    public required string Name { get; init; }

    /// <summary>Numer telefonu w formacie E.164 lub krajowym (np. +48 123 456 789).</summary>
    public required string Phone { get; init; }

    /// <summary>Adres e-mail — wymagany m.in. przez InPost dla usług paczkomatowych.</summary>
    public string? Email { get; init; }

    /// <summary>Nazwa firmy (opcjonalnie).</summary>
    public string? CompanyName { get; init; }
}
