using TBJ.Integrations.Shipping.Abstractions.Enums;

namespace TBJ.Integrations.Shipping.Abstractions.Exceptions;

/// <summary>
/// Wyjątek reprezentujący błąd zwrócony przez API firmy kurierskiej
/// lub błąd komunikacji z jej usługą.
/// </summary>
public class ShippingException : Exception
{
    /// <summary>
    /// Inicjalizuje nową instancję wyjątku z komunikatem.
    /// </summary>
    public ShippingException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Inicjalizuje nową instancję wyjątku z komunikatem i przyczyną wewnętrzną.
    /// </summary>
    public ShippingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Inicjalizuje nową instancję wyjątku z pełnymi danymi błędu kuriera.
    /// </summary>
    /// <param name="message">Opis błędu.</param>
    /// <param name="carrier">Kurier, który zwrócił błąd.</param>
    /// <param name="statusCode">Kod HTTP odpowiedzi (dla REST) lub kod błędu SOAP.</param>
    /// <param name="carrierErrorCode">Natywny kod błędu zwrócony przez API kuriera.</param>
    /// <param name="carrierErrorDetails">Szczegóły błędu zwrócone przez API kuriera.</param>
    public ShippingException(
        string message,
        CarrierType carrier,
        int? statusCode = null,
        string? carrierErrorCode = null,
        string? carrierErrorDetails = null)
        : base(message)
    {
        Carrier = carrier;
        StatusCode = statusCode;
        CarrierErrorCode = carrierErrorCode;
        CarrierErrorDetails = carrierErrorDetails;
    }

    /// <summary>Firma kurierska, której dotyczy błąd.</summary>
    public CarrierType? Carrier { get; }

    /// <summary>Kod HTTP odpowiedzi (dla REST API) lub SOAP fault code.</summary>
    public int? StatusCode { get; }

    /// <summary>Natywny kod błędu zwrócony przez API kuriera.</summary>
    public string? CarrierErrorCode { get; }

    /// <summary>Szczegółowy opis błędu zwrócony przez API kuriera.</summary>
    public string? CarrierErrorDetails { get; }
}
