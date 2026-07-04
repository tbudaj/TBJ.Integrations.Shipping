namespace TBJ.Integrations.Shipping.Carriers.FedEx.Models;

/// <summary>
/// Odpowiedź błędu FedEx API zawierająca listę błędów.
/// </summary>
internal sealed class FedExErrorResponse
{
    /// <summary>Lista błędów zwróconych przez FedEx API.</summary>
    public List<FedExApiError>? Errors { get; init; }
}

/// <summary>
/// Pojedynczy błąd FedEx API.
/// </summary>
internal sealed class FedExApiError
{
    /// <summary>Kod błędu FedEx.</summary>
    public string? Code { get; init; }

    /// <summary>Opis błędu FedEx.</summary>
    public string? Message { get; init; }
}
