using System.Globalization;
using System.Xml.Linq;

namespace TBJ.Integrations.Shipping.Carriers.GLS.Internal;

/// <summary>
/// Pomocnik parsujący odpowiedzi XML zwracane przez GLS ADE-Plus WebAPI.
/// </summary>
internal static class GlsXmlHelper
{
    /// <summary>
    /// Wyciąga identyfikator przesyłki z odpowiedzi <c>adePrepareConsignments</c>.
    /// Sprawdza kolejno elementy: <c>id</c>, <c>consignmentId</c>, <c>return</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Identyfikator przesyłki GLS.</returns>
    /// <exception cref="InvalidOperationException">Gdy nie znaleziono identyfikatora.</exception>
    public static string ParseConsignmentId(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        var value = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "id")?.Value
            ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "consignmentId")?.Value
            ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "return")?.Value;

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("GLS: brak identyfikatora przesyłki w odpowiedzi.");

        return value;
    }

    /// <summary>
    /// Wyciąga numer śledzenia przesyłki z odpowiedzi XML GLS.
    /// Sprawdza elementy: <c>parcelNumber</c>, <c>trackId</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Numer śledzenia lub null gdy brak.</returns>
    public static string? ParseTrackingNumber(string responseXml)
    {
        if (string.IsNullOrWhiteSpace(responseXml))
            return null;

        try
        {
            var doc = XDocument.Parse(responseXml);
            return doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "parcelNumber")?.Value
                ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "trackId")?.Value;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Wyciąga etykietę zakodowaną w base64 z odpowiedzi <c>adeGetConsignLabels</c>.
    /// Sprawdza elementy: <c>label</c>, <c>labelData</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Bajty etykiety lub null gdy brak zawartości.</returns>
    public static byte[]? ParseLabelBase64(string responseXml)
    {
        if (string.IsNullOrWhiteSpace(responseXml))
            return null;

        try
        {
            var doc = XDocument.Parse(responseXml);
            var base64 = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "label")?.Value
                ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "labelData")?.Value;

            return string.IsNullOrWhiteSpace(base64) ? null : Convert.FromBase64String(base64);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Wyciąga identyfikator potwierdzenia odbioru z odpowiedzi <c>adePickup_CallPickup</c>.
    /// Sprawdza elementy: <c>pickupId</c>, <c>confirmationId</c>, <c>return</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Identyfikator dyspozycji odbioru.</returns>
    /// <exception cref="InvalidOperationException">Gdy nie znaleziono identyfikatora.</exception>
    public static string ParsePickupConfirmation(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        var value = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "pickupId")?.Value
            ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "confirmationId")?.Value
            ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "return")?.Value;

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("GLS: brak identyfikatora odbioru w odpowiedzi.");

        return value;
    }

    /// <summary>
    /// Parsuje listę zdarzeń śledzenia z odpowiedzi <c>adeGetConsignStatus</c>.
    /// Przetwarza elementy: <c>events</c>/<c>event</c> ze statusem, opisem, znacznikiem czasu i depotem.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Lista krotek: (status, opis, czas, lokalizacja).</returns>
    public static IReadOnlyList<(string status, string description, DateTimeOffset time, string? location)> ParseConsignStatus(string responseXml)
    {
        var result = new List<(string, string, DateTimeOffset, string?)>();

        if (string.IsNullOrWhiteSpace(responseXml))
            return result;

        try
        {
            var doc = XDocument.Parse(responseXml);

            // Szukamy elementów event w różnych wariantach nazewnictwa
            var events = doc.Descendants()
                .Where(e => e.Name.LocalName == "event" || e.Name.LocalName == "events")
                .ToList();

            foreach (var evt in events)
            {
                var status = evt.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "status")?.Value ?? string.Empty;
                var description = evt.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "description" || e.Name.LocalName == "statusDescription")?.Value ?? string.Empty;
                var timestampStr = evt.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "timestamp" || e.Name.LocalName == "date" || e.Name.LocalName == "time")?.Value;
                var location = evt.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "depot" || e.Name.LocalName == "location" || e.Name.LocalName == "city")?.Value;

                var time = ParseTimestamp(timestampStr);
                result.Add((status, description, time, location));
            }
        }
        catch
        {
            // W przypadku błędu parsowania zwracamy pustą listę
        }

        return result;
    }

    /// <summary>
    /// Parsuje ciąg tekstowy reprezentujący znacznik czasu do <see cref="DateTimeOffset"/>.
    /// Próbuje różnych formatów, a w razie niepowodzenia zwraca bieżący czas UTC.
    /// </summary>
    private static DateTimeOffset ParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DateTimeOffset.UtcNow;

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            return dto;

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return new DateTimeOffset(dt, TimeSpan.Zero);

        return DateTimeOffset.UtcNow;
    }
}
