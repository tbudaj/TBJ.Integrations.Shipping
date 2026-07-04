using System.Globalization;
using System.Xml.Linq;

namespace TBJ.Integrations.Shipping.Carriers.DHL.Internal;

/// <summary>
/// Pomocnik parsujący odpowiedzi XML zwracane przez DHL24 WebAPI2.
/// </summary>
internal static class DhlXmlHelper
{
    private const string DhlNamespace = "http://www.dhl.com.pl/webapi2";
    private static readonly XNamespace Ns = DhlNamespace;

    /// <summary>
    /// Wyciąga identyfikator przesyłki z odpowiedzi <c>createShipments</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Identyfikator przesyłki.</returns>
    /// <exception cref="InvalidOperationException">Gdy nie znaleziono identyfikatora.</exception>
    public static string ParseShipmentId(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        var shipmentId = doc.Descendants(Ns + "shipmentId").FirstOrDefault()?.Value
            ?? doc.Descendants(Ns + "id").FirstOrDefault()?.Value;

        if (string.IsNullOrWhiteSpace(shipmentId))
            throw new InvalidOperationException("DHL: brak identyfikatora shipmentId w odpowiedzi.");

        return shipmentId;
    }

    /// <summary>
    /// Wyciąga identyfikator etykiety z odpowiedzi <c>createShipments</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Identyfikator etykiety lub null.</returns>
    public static string? ParseLabelId(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        return doc.Descendants(Ns + "labelId").FirstOrDefault()?.Value;
    }

    /// <summary>
    /// Wyciąga zawartość etykiety zakodowaną w base64 z odpowiedzi <c>createShipments</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Bajty etykiety lub null.</returns>
    public static byte[]? ParseLabelContent(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        var base64 = doc.Descendants(Ns + "labelContent").FirstOrDefault()?.Value
            ?? doc.Descendants(Ns + "label").FirstOrDefault()?.Value;

        return string.IsNullOrWhiteSpace(base64) ? null : Convert.FromBase64String(base64);
    }

    /// <summary>
    /// Wyciąga identyfikator potwierdzenia rezerwacji kuriera z odpowiedzi <c>bookCourier</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Identyfikator potwierdzenia.</returns>
    /// <exception cref="InvalidOperationException">Gdy nie znaleziono potwierdzenia.</exception>
    public static string ParseBookingConfirmation(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        var confirmation = doc.Descendants(Ns + "bookingId").FirstOrDefault()?.Value
            ?? doc.Descendants(Ns + "confirmationId").FirstOrDefault()?.Value
            ?? doc.Descendants(Ns + "shipmentId").FirstOrDefault()?.Value;

        if (string.IsNullOrWhiteSpace(confirmation))
            throw new InvalidOperationException("DHL: brak potwierdzenia rezerwacji kuriera w odpowiedzi.");

        return confirmation;
    }

    /// <summary>
    /// Parsuje historię zdarzeń śledzenia z odpowiedzi <c>getTrackAndTraceInfo</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Lista zdarzeń śledzenia.</returns>
    public static IReadOnlyList<(string status, string description, DateTimeOffset timestamp, string? location)> ParseTrackingEvents(
        string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        var events = doc.Descendants(Ns + "event").ToList();

        if (events.Count == 0)
        {
            // Próba odczytu pojedynczego elementu zamiast tablicy.
            var singleStatus = doc.Descendants(Ns + "status").FirstOrDefault()?.Value ?? string.Empty;
            var singleDescription = doc.Descendants(Ns + "description").FirstOrDefault()?.Value ?? string.Empty;
            var singleTimestamp = doc.Descendants(Ns + "timestamp").FirstOrDefault()?.Value
                ?? doc.Descendants(Ns + "time").FirstOrDefault()?.Value;
            var singleLocation = doc.Descendants(Ns + "terminal").FirstOrDefault()?.Value;

            if (!string.IsNullOrWhiteSpace(singleStatus) || !string.IsNullOrWhiteSpace(singleDescription))
            {
                var singleTime = ParseTimestamp(singleTimestamp);
                return new List<(string, string, DateTimeOffset, string?)>
                {
                    (singleStatus, singleDescription, singleTime, singleLocation),
                };
            }
        }

        var result = new List<(string, string, DateTimeOffset, string?)>();
        foreach (var evt in events)
        {
            var status = evt.Element(Ns + "status")?.Value ?? string.Empty;
            var description = evt.Element(Ns + "description")?.Value ?? string.Empty;
            var timestampText = evt.Element(Ns + "timestamp")?.Value ?? evt.Element(Ns + "time")?.Value;
            var location = evt.Element(Ns + "terminal")?.Value;

            result.Add((status, description, ParseTimestamp(timestampText), location));
        }

        return result;
    }

    /// <summary>
    /// Wyciąga dokument POD zakodowany w base64 z odpowiedzi <c>getEpod</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Bajty dokumentu POD lub null.</returns>
    public static byte[]? ParsePodBytes(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        var base64 = doc.Descendants(Ns + "podContent").FirstOrDefault()?.Value
            ?? doc.Descendants(Ns + "epod").FirstOrDefault()?.Value
            ?? doc.Descendants(Ns + "documentData").FirstOrDefault()?.Value;

        return string.IsNullOrWhiteSpace(base64) ? null : Convert.FromBase64String(base64);
    }

    /// <summary>
    /// Wyciąga nazwisko/podpis odbiorcy z odpowiedzi <c>getTrackAndTraceInfo</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Osoba odbierająca lub null.</returns>
    public static string? ParseReceivedBy(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        return doc.Descendants(Ns + "receivedBy").FirstOrDefault()?.Value;
    }

    private static DateTimeOffset ParseTimestamp(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedOffset))
                return parsedOffset;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                return new DateTimeOffset(parsedDate, TimeSpan.Zero);
        }

        return DateTimeOffset.UtcNow;
    }
}
