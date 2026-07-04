using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Carriers.GLS.Configuration;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.GLS.Internal;

/// <summary>
/// Wewnętrzny klient SOAP dla GLS ADE-Plus WebAPI.
/// Konstruuje koperty SOAP 1.1 ręcznie i wysyła je przez <see cref="HttpClient"/>.
/// </summary>
internal sealed class GlsSoapClient
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string GlsNamespace = "urn:adeplus";

    private readonly HttpClient _http;
    private readonly GlsOptions _options;
    private readonly ILogger<GlsSoapClient> _logger;

    /// <summary>
    /// Inicjalizuje klienta SOAP GLS.
    /// </summary>
    /// <param name="http">Skonfigurowany klient HTTP.</param>
    /// <param name="options">Opcje konfiguracyjne GLS.</param>
    /// <param name="logger">Logger.</param>
    public GlsSoapClient(HttpClient http, GlsOptions options, ILogger<GlsSoapClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Wywołuje operację <c>adePrepareConsignments</c> w celu rejestracji przesyłki.
    /// </summary>
    /// <param name="consignmentBodyXml">Fragment XML z danymi przesyłki.</param>
    /// <param name="ct">Token anulowania.</param>
    /// <returns>Surowa odpowiedź XML.</returns>
    public async Task<string> PrepareConsignmentsAsync(string consignmentBodyXml, GlsAuthInfo authInfo, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consignmentBodyXml);

        var bodyXml = $"<ade:adePrepareConsignments>{BuildSessionXml(authInfo)}{consignmentBodyXml}</ade:adePrepareConsignments>";
        return await PostSoapAsync("adePrepareConsignments", bodyXml, ct);
    }

    /// <summary>
    /// Wywołuje operację <c>adeGetConsignLabels</c> w celu pobrania etykiety.
    /// </summary>
    /// <param name="consignmentId">Identyfikator przesyłki GLS.</param>
    /// <param name="ct">Token anulowania.</param>
    /// <returns>Surowa odpowiedź XML z danymi etykiety.</returns>
    public async Task<string> GetConsignLabelsAsync(string consignmentId, GlsAuthInfo authInfo, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consignmentId);

        var bodyXml = $@"
<ade:adeGetConsignLabels>
    {BuildSessionXml(authInfo)}
    <ade:consign_id>{System.Net.WebUtility.HtmlEncode(consignmentId)}</ade:consign_id>
    <ade:label_type>pdf</ade:label_type>
</ade:adeGetConsignLabels>";
        return await PostSoapAsync("adeGetConsignLabels", bodyXml, ct);
    }

    /// <summary>
    /// Wywołuje operację <c>adePickup_CallPickup</c> w celu zamówienia odbioru.
    /// </summary>
    /// <param name="pickupBodyXml">Fragment XML z danymi dyspozycji odbioru.</param>
    /// <param name="ct">Token anulowania.</param>
    /// <returns>Surowa odpowiedź XML.</returns>
    public async Task<string> CallPickupAsync(string pickupBodyXml, GlsAuthInfo authInfo, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pickupBodyXml);

        var bodyXml = $"<ade:adePickup_CallPickup>{BuildSessionXml(authInfo)}{pickupBodyXml}</ade:adePickup_CallPickup>";
        return await PostSoapAsync("adePickup_CallPickup", bodyXml, ct);
    }

    /// <summary>
    /// Wywołuje operację <c>adeGetConsignStatus</c> w celu sprawdzenia statusu przesyłki.
    /// </summary>
    /// <param name="reference">Numer referencyjny lub numer śledzenia przesyłki.</param>
    /// <param name="ct">Token anulowania.</param>
    /// <returns>Surowa odpowiedź XML ze statusem przesyłki.</returns>
    public async Task<string> GetConsignStatusAsync(string reference, GlsAuthInfo authInfo, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);

        var bodyXml = $@"
<ade:adeGetConsignStatus>
    {BuildSessionXml(authInfo)}
    <ade:parcel_number>{System.Net.WebUtility.HtmlEncode(reference)}</ade:parcel_number>
</ade:adeGetConsignStatus>";
        return await PostSoapAsync("adeGetConsignStatus", bodyXml, ct);
    }

    /// <summary>
    /// Wysyła żądanie SOAP pod wskazaną akcję i zwraca surowy XML odpowiedzi.
    /// </summary>
    /// <param name="soapAction">Nazwa operacji SOAP.</param>
    /// <param name="bodyXml">Treść wewnętrzna elementu &lt;soap:Body&gt;.</param>
    /// <param name="ct">Token anulowania.</param>
    /// <returns>Surowa treść odpowiedzi XML.</returns>
    /// <exception cref="ShippingException">Gdy komunikacja lub odpowiedź SOAP zawiera błąd.</exception>
    private async Task<string> PostSoapAsync(string soapAction, string bodyXml, CancellationToken ct)
    {
        var envelope = BuildSoapEnvelope(soapAction, bodyXml);
        _logger.LogDebug("GLS SOAP: wysyłanie akcji {SoapAction} — envelope length {Length}", soapAction, envelope.Length);

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl);
        request.Headers.Add("SOAPAction", $"\"urn:adeplus#{soapAction}\"");
        request.Content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ct.IsCancellationRequested)
        {
            throw new ShippingException(
                $"Przekroczono limit czasu oczekiwania na odpowiedź GLS dla akcji {soapAction}: {ex.Message}",
                CarrierType.GLS);
        }
        catch (HttpRequestException ex)
        {
            throw new ShippingException(
                $"Błąd komunikacji z GLS dla akcji {soapAction}: {ex.Message}",
                CarrierType.GLS,
                carrierErrorDetails: ex.Message);
        }

        _logger.LogInformation(
            "GLS SOAP: {SoapAction} — HTTP {StatusCode}",
            soapAction,
            (int)response.StatusCode);

        var responseXml = await response.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("GLS SOAP: {SoapAction} — response body length {Length}", soapAction, responseXml.Length);

        if (!response.IsSuccessStatusCode)
        {
            var faultString = ParseSoapFaultString(responseXml);
            throw new ShippingException(
                $"GLS API zwróciło błąd HTTP {(int)response.StatusCode} dla akcji {soapAction}: {faultString}",
                CarrierType.GLS,
                statusCode: (int)response.StatusCode,
                carrierErrorDetails: faultString);
        }

        return responseXml;
    }

    /// <summary>
    /// Buduje pełną kopertę SOAP 1.1 z przestrzenią nazw GLS ADE-Plus.
    /// </summary>
    private static string BuildSoapEnvelope(string methodName, string bodyContent)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNamespace}"" xmlns:ade=""{GlsNamespace}"">
    <soap:Header />
    <soap:Body>
        {bodyContent}
    </soap:Body>
</soap:Envelope>";
    }

    /// <summary>
    /// Buduje element sesji z danymi uwierzytelnienia GLS.
    /// </summary>
    private static string BuildSessionXml(GlsAuthInfo authInfo)
    {
        return $@"<ade:session>
    <ade:user>{System.Net.WebUtility.HtmlEncode(authInfo.Username)}</ade:user>
    <ade:pass>{System.Net.WebUtility.HtmlEncode(authInfo.Password)}</ade:pass>
</ade:session>";
    }

    /// <summary>
    /// Parsuje treść błędu SOAP z elementu &lt;faultstring&gt;.
    /// </summary>
    private static string ParseSoapFaultString(string responseXml)
    {
        if (string.IsNullOrWhiteSpace(responseXml))
            return "Brak treści odpowiedzi.";

        try
        {
            var doc = XDocument.Parse(responseXml);
            var faultString = doc.Descendants(XName.Get("faultstring")).FirstOrDefault()?.Value
                ?? doc.Descendants(XName.Get("faultString")).FirstOrDefault()?.Value;
            return faultString ?? responseXml;
        }
        catch
        {
            return responseXml;
        }
    }
}
