using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using TBJ.Integrations.Shipping.Abstractions.Auth;
using TBJ.Integrations.Shipping.Abstractions.Enums;
using TBJ.Integrations.Shipping.Abstractions.Exceptions;
using TBJ.Integrations.Shipping.Carriers.DPD.Configuration;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping.Carriers.DPD.Internal;

/// <summary>
/// Wewnętrzny klient SOAP dla DPD Web Service.
/// Konstruuje koperty SOAP 1.1 ręcznie i wysyła je przez <see cref="HttpClient"/>.
/// Dane uwierzytelniające są przekazywane per-żądanie przez <see cref="DpdAuthInfo"/>.
/// </summary>
internal sealed class DpdSoapClient
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string DpdNamespace = "http://www.dpdportal.pl/schema/webapi";

    private readonly HttpClient _http;
    private readonly DpdOptions _options;
    private readonly ILogger<DpdSoapClient> _logger;

    /// <summary>
    /// Inicjalizuje klienta SOAP DPD.
    /// </summary>
    /// <param name="http">Skonfigurowany klient HTTP.</param>
    /// <param name="options">Opcje infrastrukturalne DPD.</param>
    /// <param name="logger">Logger.</param>
    public DpdSoapClient(HttpClient http, DpdOptions options, ILogger<DpdSoapClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Wysyła żądanie SOAP pod wskazaną akcję i zwraca surowy XML odpowiedzi.
    /// </summary>
    /// <param name="soapAction">Nazwa operacji SOAP w przestrzeni nazw DPD.</param>
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
        _logger.LogDebug("DPD SOAP: wysyłanie akcji {SoapAction} — envelope length {Length}", soapAction, envelope.Length);

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl);
        request.Headers.Add("SOAPAction", $"\"{DpdNamespace}/{soapAction}\"");
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
                $"Przekroczono limit czasu oczekiwania na odpowiedź DPD dla akcji {soapAction}: {ex.Message}",
                CarrierType.DPD);
        }
        catch (HttpRequestException ex)
        {
            throw new ShippingException(
                $"Błąd komunikacji z DPD dla akcji {soapAction}: {ex.Message}",
                CarrierType.DPD);
        }

        _logger.LogInformation(
            "DPD SOAP: {SoapAction} — HTTP {StatusCode}",
            soapAction,
            (int)response.StatusCode);

        var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("DPD SOAP: {SoapAction} — response body length {Length}", soapAction, responseXml.Length);

        if (!response.IsSuccessStatusCode || ContainsSoapFault(responseXml))
        {
            ThrowSoapFault(responseXml, soapAction, (int)response.StatusCode);
        }

        return responseXml;
    }

    /// <summary>
    /// Wywołuje operację <c>generatePackagesNumbersV1</c>.
    /// </summary>
    /// <param name="openUmlfXml">Fragment XML zawierający element &lt;openUMLF&gt;.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Surowa odpowiedź XML.</returns>
    public async Task<string> GeneratePackagesNumbersAsync(
        string openUmlfXml,
        DpdAuthInfo authInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(openUmlfXml);

        var bodyXml = $"<ns:generatePackagesNumbersV1>{BuildAuthParams(authInfo)}{openUmlfXml}</ns:generatePackagesNumbersV1>";
        return await PostSoapAsync("generatePackagesNumbersV1", bodyXml, cancellationToken);
    }

    /// <summary>
    /// Wywołuje operację <c>generateSpedLabelsV1</c>.
    /// </summary>
    /// <param name="pkgIdList">Fragment XML z listą identyfikatorów paczek.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Surowa odpowiedź XML zawierająca etykietę base64.</returns>
    public async Task<string> GenerateLabelsAsync(
        string pkgIdList,
        DpdAuthInfo authInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pkgIdList);

        var bodyXml = $@"
<ns:generateSpedLabelsV1>
    {BuildAuthParams(authInfo)}
    {pkgIdList}
    <ns:outputDocFormat>{WebUtility.HtmlEncode(_options.LabelFormat)}</ns:outputDocFormat>
    <ns:outputDocPageFormat>{WebUtility.HtmlEncode(_options.LabelPageFormat)}</ns:outputDocPageFormat>
</ns:generateSpedLabelsV1>";
        return await PostSoapAsync("generateSpedLabelsV1", bodyXml, cancellationToken);
    }

    /// <summary>
    /// Wywołuje operację <c>packagesPickupCallV1</c>.
    /// </summary>
    /// <param name="pickupXml">Fragment XML z danymi dyspozycji odbioru.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Surowa odpowiedź XML.</returns>
    public async Task<string> PackagesPickupCallAsync(
        string pickupXml,
        DpdAuthInfo authInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pickupXml);

        var bodyXml = $"<ns:packagesPickupCallV1>{BuildAuthParams(authInfo)}{pickupXml}</ns:packagesPickupCallV1>";
        return await PostSoapAsync("packagesPickupCallV1", bodyXml, cancellationToken);
    }

    /// <summary>
    /// Wywołuje operację <c>findPackageStatusV1</c>.
    /// </summary>
    /// <param name="waybill">Numer listu przewozowego.</param>
    /// <param name="authInfo">Dane uwierzytelniające tenanta.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Surowa odpowiedź XML.</returns>
    public async Task<string> FindPackageStatusAsync(
        string waybill,
        DpdAuthInfo authInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(waybill);

        var bodyXml = $@"
<ns:findPackageStatusV1>
    {BuildAuthParams(authInfo)}
    <ns:waybill>{WebUtility.HtmlEncode(waybill)}</ns:waybill>
</ns:findPackageStatusV1>";
        return await PostSoapAsync("findPackageStatusV1", bodyXml, cancellationToken);
    }

    /// <summary>
    /// Buduje pełną kopertę SOAP 1.1 z przestrzenią nazw DPD.
    /// </summary>
    private static string BuildSoapEnvelope(string soapAction, string bodyXml)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNamespace}"" xmlns:ns=""{DpdNamespace}"">
    <soap:Header />
    <soap:Body>
        {bodyXml}
    </soap:Body>
</soap:Envelope>";
    }

    /// <summary>
    /// Buduje parametry uwierzytelnienia wymagane przez większość operacji DPD.
    /// </summary>
    private static string BuildAuthParams(DpdAuthInfo authInfo)
    {
        return $@"
<ns:dpdServicesParamsV1>
    <ns:authDataV1>
        <ns:username>{WebUtility.HtmlEncode(authInfo.Username)}</ns:username>
        <ns:password>{WebUtility.HtmlEncode(authInfo.Password)}</ns:password>
    </ns:authDataV1>
    <ns:masterFid>{authInfo.Fid}</ns:masterFid>
    <ns:slaveFid>{authInfo.Fid}</ns:slaveFid>
    <ns:langCode>PL</ns:langCode>
</ns:dpdServicesParamsV1>";
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

        var message = faultString ?? $"DPD zwróciło błąd dla akcji {soapAction}.";
        throw new ShippingException(
            message,
            CarrierType.DPD,
            httpStatusCode,
            faultCode,
            details ?? responseXml);
    }
}
