namespace TBJ.Integrations.Shipping.Carriers.FedEx.Models;

/// <summary>
/// Odpowiedź FedEx Track API ze statusem i historią przesyłki.
/// </summary>
internal sealed class FedExTrackResponse
{
    /// <summary>Dane wyjściowe odpowiedzi śledzenia.</summary>
    public FedExTrackOutput? Output { get; init; }
}

/// <summary>
/// Dane wyjściowe odpowiedzi śledzenia FedEx.
/// </summary>
internal sealed class FedExTrackOutput
{
    /// <summary>Lista kompletnych wyników śledzenia.</summary>
    public List<FedExCompleteTrackResult>? CompleteTrackResults { get; init; }
}

/// <summary>
/// Kompletny wynik śledzenia przesyłki FedEx.
/// </summary>
internal sealed class FedExCompleteTrackResult
{
    /// <summary>Lista wyników śledzenia dla numeru.</summary>
    public List<FedExTrackResult>? TrackResults { get; init; }
}

/// <summary>
/// Wynik śledzenia przesyłki FedEx.
/// </summary>
internal sealed class FedExTrackResult
{
    /// <summary>Ostatni (aktualny) status przesyłki.</summary>
    public FedExLatestStatusDetail? LatestStatusDetail { get; init; }

    /// <summary>Lista dat i czasów powiązanych ze statusami.</summary>
    public List<FedExDateAndTime>? DateAndTimes { get; init; }

    /// <summary>Lista zdarzeń skanowania (historia śledzenia).</summary>
    public List<FedExScanEvent>? ScanEvents { get; init; }

    /// <summary>Informacje o podpisie/POD.</summary>
    public FedExSignatureInformation? SignatureInformation { get; init; }
}

/// <summary>
/// Szczegóły ostatniego statusu przesyłki FedEx.
/// </summary>
internal sealed class FedExLatestStatusDetail
{
    /// <summary>Kod statusu.</summary>
    public string? Code { get; init; }

    /// <summary>Opis statusu.</summary>
    public string? Description { get; init; }

    /// <summary>Wyprowadzony kod statusu (bardziej szczegółowy).</summary>
    public string? DerivedCode { get; init; }
}

/// <summary>
/// Data i czas FedEx.
/// </summary>
internal sealed class FedExDateAndTime
{
    /// <summary>Typ daty (np. ACTUAL_DELIVERY, ESTIMATED_DELIVERY).</summary>
    public string? Type { get; init; }

    /// <summary>Data i czas w formacie ISO 8601.</summary>
    public string? DateTime { get; init; }
}

/// <summary>
/// Zdarzenie skanowania w historii przesyłki FedEx.
/// </summary>
internal sealed class FedExScanEvent
{
    /// <summary>Data zdarzenia (format yyyy-MM-dd).</summary>
    public string? Date { get; init; }

    /// <summary>Czas zdarzenia (format HH:mm:ss).</summary>
    public string? Time { get; init; }

    /// <summary>Typ zdarzenia FedEx.</summary>
    public string? EventType { get; init; }

    /// <summary>Opis zdarzenia.</summary>
    public string? EventDescription { get; init; }

    /// <summary>Wyprowadzony kod statusu zdarzenia.</summary>
    public string? DerivedStatusCode { get; init; }

    /// <summary>Lokalizacja zdarzenia.</summary>
    public FedExScanLocation? ScanLocation { get; init; }
}

/// <summary>
/// Lokalizacja zdarzenia skanowania FedEx.
/// </summary>
internal sealed class FedExScanLocation
{
    /// <summary>Miasto.</summary>
    public string? City { get; init; }

    /// <summary>Kod kraju.</summary>
    public string? CountryCode { get; init; }
}

/// <summary>
/// Informacje o podpisie/POD przesyłki FedEx.
/// </summary>
internal sealed class FedExSignatureInformation
{
    /// <summary>Nazwa pliku sformatowanego podpisu (może zawierać imię odbiorcy).</summary>
    public string? FormattedSignatureFileName { get; init; }
}
