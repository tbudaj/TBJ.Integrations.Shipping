namespace TBJ.Integrations.Shipping.Carriers.UPS.Models;

/// <summary>
/// Odpowiedź błędu UPS API zawierająca listę błędów.
/// </summary>
internal sealed class UpsErrorResponse
{
    /// <summary>Kontener odpowiedzi z błędami.</summary>
    public UpsErrorResponseBody? Response { get; init; }
}

/// <summary>
/// Treść odpowiedzi błędu UPS.
/// </summary>
internal sealed class UpsErrorResponseBody
{
    /// <summary>Lista błędów zwróconych przez UPS API.</summary>
    public List<UpsApiError>? Errors { get; init; }
}

/// <summary>
/// Pojedynczy błąd UPS API.
/// </summary>
internal sealed class UpsApiError
{
    /// <summary>Kod błędu UPS.</summary>
    public string? Code { get; init; }

    /// <summary>Opis błędu UPS.</summary>
    public string? Message { get; init; }
}
