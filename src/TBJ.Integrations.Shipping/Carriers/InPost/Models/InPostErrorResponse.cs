namespace TBJ.Integrations.Shipping.Carriers.InPost.Models;

/// <summary>
/// Odpowiedź błędu z InPost ShipX API.
/// </summary>
internal sealed class InPostErrorResponse
{
    /// <summary>Kod statusu HTTP zwrócony przez API.</summary>
    public int? Status { get; set; }

    /// <summary>Kod błędu (np. <c>resource_not_found</c>).</summary>
    public string? Error { get; set; }

    /// <summary>Czytelny komunikat błędu.</summary>
    public string? Message { get; set; }

    /// <summary>Szczegółowe informacje o błędzie (np. walidacja pól).</summary>
    public List<InPostErrorDetail>? Details { get; set; }
}

/// <summary>
/// Szczegóły błędu walidacji zwrócone przez InPost ShipX API.
/// </summary>
internal sealed class InPostErrorDetail
{
    /// <summary>Nazwa pola, którego dotyczy błąd.</summary>
    public string? Field { get; set; }

    /// <summary>Komunikat błędu dla pola.</summary>
    public string? Message { get; set; }
}
