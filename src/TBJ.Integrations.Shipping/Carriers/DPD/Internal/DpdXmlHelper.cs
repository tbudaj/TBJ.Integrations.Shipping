using System.Globalization;
using System.Xml.Linq;

namespace TBJ.Integrations.Shipping.Carriers.DPD.Internal;

/// <summary>
/// Pomocnik parsujący odpowiedzi XML zwracane przez DPD Web Service.
/// </summary>
internal static class DpdXmlHelper
{
    private const string DpdNamespace = "http://www.dpdportal.pl/schema/webapi";
    private static readonly XNamespace Ns = DpdNamespace;

    /// <summary>
    /// Wyciąga identyfikator paczki (<c>packageId</c>) z odpowiedzi <c>generatePackagesNumbersV1</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Identyfikator paczki.</returns>
    /// <exception cref="InvalidOperationException">Gdy nie znaleziono identyfikatora.</exception>
    public static string ParsePackageId(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        var packageId = doc.Descendants(Ns + "packageId").FirstOrDefault()?.Value;

        if (string.IsNullOrWhiteSpace(packageId))
            throw new InvalidOperationException("DPD: brak identyfikatora packageId w odpowiedzi.");

        return packageId;
    }

    /// <summary>
    /// Wyciąga numer listu przewozowego (<c>waybill</c>) z odpowiedzi <c>generatePackagesNumbersV1</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Numer listu przewozowego.</returns>
    /// <exception cref="InvalidOperationException">Gdy nie znaleziono numeru listu.</exception>
    public static string ParseWaybill(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        var waybill = doc.Descendants(Ns + "waybill").FirstOrDefault()?.Value;

        if (string.IsNullOrWhiteSpace(waybill))
            throw new InvalidOperationException("DPD: brak numeru waybill w odpowiedzi.");

        return waybill;
    }

    /// <summary>
    /// Wyciąga zawartość etykiety zakodowaną w base64 z odpowiedzi <c>generateSpedLabelsV1</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Bajty etykiety lub null gdy brak zawartości.</returns>
    public static byte[]? ParseLabelBase64(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        var base64 = doc.Descendants(Ns + "labelContent").FirstOrDefault()?.Value
            ?? doc.Descendants(Ns + "documentData").FirstOrDefault()?.Value
            ?? doc.Descendants(Ns + "label").FirstOrDefault()?.Value;

        return string.IsNullOrWhiteSpace(base64) ? null : Convert.FromBase64String(base64);
    }

    /// <summary>
    /// Wyciąga identyfikator dokumentu odbioru (<c>documentId</c>) z odpowiedzi <c>packagesPickupCallV1</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Identyfikator dokumentu odbioru.</returns>
    /// <exception cref="InvalidOperationException">Gdy nie znaleziono identyfikatora.</exception>
    public static string ParsePickupDocumentId(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        var documentId = doc.Descendants(Ns + "documentId").FirstOrDefault()?.Value
            ?? doc.Descendants(Ns + "statusInfo").FirstOrDefault()?.Value;

        if (string.IsNullOrWhiteSpace(documentId))
            throw new InvalidOperationException("DPD: brak identyfikatora documentId w odpowiedzi odbioru.");

        return documentId;
    }

    /// <summary>
    /// Parsuje status przesyłki z odpowiedzi <c>findPackageStatusV1</c>.
    /// </summary>
    /// <param name="responseXml">Surowa odpowiedź XML.</param>
    /// <returns>Krotka (kod statusu, opis, czas zdarzenia).</returns>
    public static (string state, string description, DateTime? eventTime) ParseTrackingStatus(string responseXml)
    {
        var doc = XDocument.Parse(responseXml);
        var state = doc.Descendants(Ns + "state").FirstOrDefault()?.Value ?? string.Empty;
        var description = doc.Descendants(Ns + "description").FirstOrDefault()?.Value ?? string.Empty;

        var eventTimeText = doc.Descendants(Ns + "eventTime").FirstOrDefault()?.Value;
        DateTime? eventTime = null;
        if (!string.IsNullOrWhiteSpace(eventTimeText) &&
            DateTime.TryParse(eventTimeText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            eventTime = parsed;
        }

        return (state, description, eventTime);
    }
}
