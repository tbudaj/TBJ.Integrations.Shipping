using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Carriers.DHL.Configuration;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.DHL.Internal;

/// <summary>
/// Wewnętrzny klient SOAP dla DHL24 WebAPI2.
/// Konstruuje koperty SOAP 1.1 ręcznie i wysyła je przez <see cref="HttpClient"/>.
/// </summary>
internal sealed class DhlSoapClient
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string DhlNamespace = "http://www.dhl.com.pl/webapi2";

    private readonly HttpClient _http;
    private readonly DhlOptions _options;
    private readonly ILogger<DhlSoapClient> _logger;

    /// <summary>
    /// Inicjalizuje klienta SOAP DHL.
    /// </summary>
    /// <param name="http">Skonfigurowany klient HTTP.</param>
    /// <param name="options">Opcje DHL.</param>
    /// <param name="logger">Logger.</param>
    public DhlSoapClient(HttpClient http, DhlOptions options, ILogger<DhlSoapClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Wysyła żądanie SOAP pod wskazaną akcję i zwraca surowy XML odpowiedzi.
    /// </summary>
    /// <param name="soapAction">Nazwa operacji SOAP w przestrzeni nazw DHL.</param>
    /// <param name="bodyXml">Treść wewnętrzna elementu &lt;soap:Body&gt;.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Surowa treść odpowiedzi XML.</returns>
    /// <exception cref="ShippingException">Gdy komunikacja lub odpowiedź SOAP zawiera błąd.</exception>
    public async Task<string> PostSoapAsync(
        string soapAction,
        string bodyXml,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(soapAction);
        ArgumentException.ThrowIfNullOrWhiteSpace(bodyXml);

        var envelope = BuildSoapEnvelope(soapAction, bodyXml);
        _logger.LogDebug("DHL SOAP: wysyłanie akcji {SoapAction} — envelope length {Length}", soapAction, envelope.Length);

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl);
        request.Headers.Add("SOAPAction", $"\"{DhlNamespace}/{soapAction}\"");
        request.Content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !cancellationToken.IsCancellationRequested)
        {
            throw new ShippingException(
                $"Przekroczono limit czasu oczekiwania na odpowiedź DHL dla akcji {soapAction}: {ex.Message}",
                CarrierType.DHL);
        }
        catch (HttpRequestException ex)
        {
            throw new ShippingException(
                $"Błąd komunikacji z DHL dla akcji {soapAction}: {ex.Message}",
                CarrierType.DHL);
        }

        _logger.LogInformation(
            "DHL SOAP: {SoapAction} — HTTP {StatusCode}",
            soapAction,
            (int)response.StatusCode);

        var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("DHL SOAP: {SoapAction} — response body length {Length}", soapAction, responseXml.Length);

        if (!response.IsSuccessStatusCode || ContainsSoapFault(responseXml))
        {
            ThrowSoapFault(responseXml, soapAction, (int)response.StatusCode);
        }

        return responseXml;
    }

    /// <summary>
    /// Wywołuje operację <c>createShipments</c>.
    /// </summary>
    /// <param name="shipmentsXml">Fragment XML zawierający przesyłki.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Surowa odpowiedź XML.</returns>
    public async Task<string> CreateShipmentsAsync(
        string shipmentsXml,
        DhlAuthInfo authInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shipmentsXml);

        var bodyXml = $"<ns:createShipments>{BuildAuthDataXml(authInfo)}{shipmentsXml}</ns:createShipments>";
        return await PostSoapAsync("createShipments", bodyXml, cancellationToken);
    }

    /// <summary>
    /// Wywołuje operację <c>bookCourier</c>.
    /// </summary>
    /// <param name="bookCourierXml">Fragment XML z danymi rezerwacji kuriera.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Surowa odpowiedź XML.</returns>
    public async Task<string> BookCourierAsync(
        string bookCourierXml,
        DhlAuthInfo authInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookCourierXml);

        var bodyXml = $"<ns:bookCourier>{BuildAuthDataXml(authInfo)}{bookCourierXml}</ns:bookCourier>";
        return await PostSoapAsync("bookCourier", bodyXml, cancellationToken);
    }

    /// <summary>
    /// Wywołuje operację <c>getTrackAndTraceInfo</c>.
    /// </summary>
    /// <param name="shipmentId">Identyfikator przesyłki.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Surowa odpowiedź XML.</returns>
    public async Task<string> GetTrackAndTraceInfoAsync(
        string shipmentId,
        DhlAuthInfo authInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shipmentId);

        var bodyXml = $@"
<ns:getTrackAndTraceInfo>
    {BuildAuthDataXml(authInfo)}
    <ns:shipmentId>{WebUtility.HtmlEncode(shipmentId)}</ns:shipmentId>
</ns:getTrackAndTraceInfo>";
        return await PostSoapAsync("getTrackAndTraceInfo", bodyXml, cancellationToken);
    }

    /// <summary>
    /// Wywołuje operację <c>getEpod</c>.
    /// </summary>
    /// <param name="shipmentId">Identyfikator przesyłki.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Surowa odpowiedź XML.</returns>
    public async Task<string> GetEpodAsync(
        string shipmentId,
        DhlAuthInfo authInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shipmentId);

        var bodyXml = $@"
<ns:getEpod>
    {BuildAuthDataXml(authInfo)}
    <ns:shipmentId>{WebUtility.HtmlEncode(shipmentId)}</ns:shipmentId>
</ns:getEpod>";
        return await PostSoapAsync("getEpod", bodyXml, cancellationToken);
    }

    /// <summary>
    /// Buduje pełną kopertę SOAP 1.1 z przestrzenią nazw DHL.
    /// </summary>
    private static string BuildSoapEnvelope(string soapAction, string bodyXml)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNamespace}"" xmlns:ns=""{DhlNamespace}"">
    <soap:Header />
    <soap:Body>
        {bodyXml}
    </soap:Body>
</soap:Envelope>";
    }

    /// <summary>
    /// Buduje dane uwierzytelnienia <c>authData</c> wymagane przez każdą operację DHL.
    /// </summary>
    private static string BuildAuthDataXml(DhlAuthInfo authInfo)
    {
        return $@"
<ns:authData>
    <ns:login>{WebUtility.HtmlEncode(authInfo.Login)}</ns:login>
    <ns:password>{WebUtility.HtmlEncode(authInfo.Password)}</ns:password>
</ns:authData>";
    }

    /// <summary>
    /// Sprawdza, czy odpowiedź XML zawiera element &lt;soap:Fault&gt;.
    /// </summary>
    private static bool ContainsSoapFault(string responseXml)
    {
        if (string.IsNullOrWhiteSpace(responseXml))
            return false;

        try
        {
            var doc = XDocument.Parse(responseXml);
            return doc.Descendants(XName.Get("Fault", SoapNamespace)).Any();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parsuje błąd SOAP i rzuca wyjątek <see cref="ShippingException"/>.
    /// </summary>
    private static void ThrowSoapFault(string responseXml, string soapAction, int httpStatusCode)
    {
        string? faultCode = null;
        string? faultString = null;
        string? details = null;

        if (!string.IsNullOrWhiteSpace(responseXml))
        {
            try
            {
                var doc = XDocument.Parse(responseXml);
                var fault = doc.Descendants(XName.Get("Fault", SoapNamespace)).FirstOrDefault();
                if (fault != null)
                {
                    faultCode = fault.Element(XName.Get("faultcode", SoapNamespace))?.Value;
                    faultString = fault.Element(XName.Get("faultstring", SoapNamespace))?.Value;
                    details = fault.Element(XName.Get("detail", SoapNamespace))?.Value;
                }
            }
            catch
            {
                // ignored — użyjemy surowej odpowiedzi jako szczegółu błędu.
            }
        }

        var message = faultString ?? $"DHL zwróciło błąd dla akcji {soapAction}.";
        throw new ShippingException(
            message,
            CarrierType.DHL,
            httpStatusCode,
            faultCode,
            details ?? responseXml);
    }
}
