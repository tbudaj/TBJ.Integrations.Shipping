using TBJ.Integrations.Shipping.Carriers.DPD;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Carriers.DPD.Configuration;
using TBJ.Integrations.Shipping.Carriers.DPD.Internal;
using TBJ.Integrations.Shipping.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping;

/// <summary>
/// Rozszerzenia rejestracji adaptera DPD w bramie wysyłkowej.
/// </summary>
public static class DpdExtensions
{
    /// <summary>
    /// Rejestruje adapter kurierski DPD w <see cref="ShippingGatewayBuilder"/>.
    /// </summary>
    /// <param name="builder">Budowniczy bramy wysyłkowej.</param>
    /// <param name="configure">Akcja konfiguracji opcji infrastrukturalnych DPD (BaseUrl, Timeout, LabelFormat).</param>
    /// <returns>Budowniczy bramy wysyłkowej (fluent API).</returns>
    /// <remarks>
    /// Obsługuje dwa scenariusze uwierzytelniania:
    /// <list type="bullet">
    /// <item><description>
    /// <b>Scenariusz A (konto tenanta):</b> przekaż <c>DpdAuthInfo</c> w każdym żądaniu.
    /// </description></item>
    /// <item><description>
    /// <b>Scenariusz B (nasze konto):</b> ustaw <c>DefaultUsername</c>, <c>DefaultPassword</c>
    /// i <c>DefaultFid</c> w opcjach — używane automatycznie gdy żądanie nie zawiera <c>DpdAuthInfo</c>.
    /// </description></item>
    /// </list>
    /// </remarks>
    public static ShippingGatewayBuilder AddDPD(
        this ShippingGatewayBuilder builder,
        Action<DpdOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new DpdOptions();
        configure?.Invoke(options);
        ValidateOptions(options);

        builder.Services.AddSingleton(options);

        builder.Services.AddHttpClient<DpdSoapClient>((_, client) =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = options.Timeout;
        });

        builder.Services.AddScoped<IShippingClient>(sp =>
            new DpdShippingClient(
                sp.GetRequiredService<DpdSoapClient>(),
                sp.GetRequiredService<DpdOptions>(),
                sp.GetRequiredService<ILogger<DpdShippingClient>>()));

        return builder;
    }

    /// <summary>
    /// Rejestruje adapter kurierski DPD w <see cref="ShippingGatewayBuilder"/>,
    /// odczytując konfigurację z sekcji <see cref="DpdOptions.SectionName"/>.
    /// </summary>
    /// <param name="builder">Budowniczy bramy wysyłkowej.</param>
    /// <param name="configuration">Konfiguracja aplikacji.</param>
    /// <returns>Budowniczy bramy wysyłkowej (fluent API).</returns>
    public static ShippingGatewayBuilder AddDPD(
        this ShippingGatewayBuilder builder,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(DpdOptions.SectionName);
        return builder.AddDPD(opt =>
        {
            var baseUrl = section["BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
                opt.BaseUrl = baseUrl;

            var timeout = section["Timeout"];
            if (!string.IsNullOrWhiteSpace(timeout) && TimeSpan.TryParse(timeout, out var ts))
                opt.Timeout = ts;

            var labelFormat = section["LabelFormat"];
            if (!string.IsNullOrWhiteSpace(labelFormat))
                opt.LabelFormat = labelFormat;

            var labelPageFormat = section["LabelPageFormat"];
            if (!string.IsNullOrWhiteSpace(labelPageFormat))
                opt.LabelPageFormat = labelPageFormat;

            // Scenariusz B: domyślne credentials naszego konta (opcjonalne)
            var defaultUsername = section[nameof(DpdOptions.DefaultUsername)];
            if (!string.IsNullOrWhiteSpace(defaultUsername))
                opt.DefaultUsername = defaultUsername;

            var defaultPassword = section[nameof(DpdOptions.DefaultPassword)];
            if (!string.IsNullOrWhiteSpace(defaultPassword))
                opt.DefaultPassword = defaultPassword;

            var defaultFid = section[nameof(DpdOptions.DefaultFid)];
            if (!string.IsNullOrWhiteSpace(defaultFid) && int.TryParse(defaultFid, out var fid))
                opt.DefaultFid = fid;
        });
    }

    private static void ValidateOptions(DpdOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new InvalidOperationException(
                $"Brak konfiguracji {nameof(DpdOptions.BaseUrl)} dla DPD Web Service.");

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException(
                $"Nieprawidłowy URL '{options.BaseUrl}' w konfiguracji {nameof(DpdOptions.BaseUrl)}.");

        if (options.Timeout <= TimeSpan.Zero)
            throw new InvalidOperationException(
                $"Wartość {nameof(DpdOptions.Timeout)} musi być większa od zera.");
    }
}
