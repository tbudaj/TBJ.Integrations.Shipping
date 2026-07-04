namespace TBJ.Integrations.Shipping.Abstractions.Models;

/// <summary>
/// Etykieta wysyłkowa zwrócona przez kuriera po rejestracji przesyłki.
/// Zawiera surowe bajty dokumentu oraz typ zawartości (MIME).
/// </summary>
public sealed class LabelResult
{
    /// <summary>
    /// Surowe bajty dokumentu etykiety.
    /// Może to być PDF, ZPL, EPL lub inny format w zależności od kuriera i konfiguracji.
    /// </summary>
    public required byte[] Content { get; init; }

    /// <summary>
    /// Typ MIME zawartości, np. <c>application/pdf</c>, <c>application/zpl</c>.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Sugerowana nazwa pliku etykiety (opcjonalna), np. <c>label_123456789.pdf</c>.
    /// </summary>
    public string? FileName { get; init; }
}
