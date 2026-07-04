namespace TBJ.Integrations.Shipping.Carriers.FedEx.Models;

/// <summary>
/// Odpowiedź FedEx Ship API przy rejestracji przesyłki.
/// </summary>
internal sealed class FedExShipResponse
{
    /// <summary>Dane wyjściowe odpowiedzi.</summary>
    public FedExShipOutput? Output { get; init; }
}

/// <summary>
/// Dane wyjściowe odpowiedzi FedEx Ship API.
/// </summary>
internal sealed class FedExShipOutput
{
    /// <summary>Lista zarejestrowanych przesyłek.</summary>
    public List<FedExTransactionShipment>? TransactionShipments { get; init; }
}

/// <summary>
/// Transakcja nadania przesyłki FedEx.
/// </summary>
internal sealed class FedExTransactionShipment
{
    /// <summary>Główny numer śledzenia przesyłki.</summary>
    public string? MasterTrackingNumber { get; init; }

    /// <summary>Typ usługi przesyłki.</summary>
    public string? ServiceType { get; init; }

    /// <summary>Lista odpowiedzi dla poszczególnych paczek.</summary>
    public List<FedExPieceResponse>? PieceResponses { get; init; }
}

/// <summary>
/// Odpowiedź dla pojedynczej paczki FedEx.
/// </summary>
internal sealed class FedExPieceResponse
{
    /// <summary>Lista dokumentów paczki (etykiety).</summary>
    public List<FedExPackageDocument>? PackageDocuments { get; init; }
}

/// <summary>
/// Dokument paczki FedEx (etykieta).
/// </summary>
internal sealed class FedExPackageDocument
{
    /// <summary>Etykieta paczki zakodowana w base64.</summary>
    public string? EncodedLabel { get; init; }

    /// <summary>Typ zawartości dokumentu.</summary>
    public string? ContentType { get; init; }
}
