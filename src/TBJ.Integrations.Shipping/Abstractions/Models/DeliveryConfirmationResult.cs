namespace TBJ.Integrations.Shipping.Abstractions.Models;

/// <summary>
/// Wynik potwierdzenia doręczenia przesyłki (Proof of Delivery — POD).
/// </summary>
public sealed class DeliveryConfirmationResult
{
    /// <summary>Numer śledzenia przesyłki.</summary>
    public required string TrackingNumber { get; init; }

    /// <summary>Data i czas doręczenia (UTC).</summary>
    public required DateTimeOffset DeliveredAt { get; init; }

    /// <summary>Imię i nazwisko/podpis osoby, która odebrała przesyłkę (jeśli dostępne).</summary>
    public string? ReceivedBy { get; init; }

    /// <summary>
    /// Dokument POD jako bajty (np. skan podpisu w PDF lub HTML).
    /// Null jeśli kurier nie udostępnia dokumentu POD w formacie cyfrowym.
    /// </summary>
    public byte[]? PodDocument { get; init; }

    /// <summary>Typ MIME dokumentu POD (np. <c>application/pdf</c>, <c>text/html</c>).</summary>
    public string? PodContentType { get; init; }

    /// <summary>
    /// Surowe dane zwrócone przez kuriera (do celów diagnostycznych / audytu).
    /// </summary>
    public string? RawCarrierResponse { get; init; }
}
