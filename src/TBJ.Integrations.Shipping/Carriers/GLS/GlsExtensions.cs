using TBJ.Integrations.Shipping.Carriers.GLS;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Carriers.GLS.Configuration;
using TBJ.Integrations.Shipping.Carriers.GLS.Internal;
using TBJ.Integrations.Shipping.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping;

/// <summary>
/// Rozszerzenia rejestracji adaptera GLS w bramie wysyłkowej.
/// </summary>
public static class GlsExtensions
{
    /// <summary>
    /// Rejestruje adapter kurierski GLS w <see cref="ShippingGatewayBuilder"/>.
    /// </summary>
    /// <param name="builder">Budowniczy bramy wysyłkowej.</param>
    /// <param name="configure">Akcja konfiguracji opcji infrastrukturalnych GLS (BaseUrl, Timeout, CountryCode).</param>
    /// <returns>Budowniczy bramy wysyłkowej (fluent API).</returns>
    /// <remarks>
    /// <b>Scenariusz A (wielotenantowy):</b> dane uwierzytelniające (Username, Password) są specyficzne
    /// dla tenanta i przekazywane per-żądanie przez <c>GlsAuthInfo</c> w <see cref="CreateShipmentRequest"/>.
    /// <b>Scenariusz B (single-tenant):</b> ustaw <see cref="GlsOptions.DefaultUsername"/>
    /// i <see cref="GlsOptions.DefaultPassword"/> — używane gdy żądanie nie zawiera <c>GlsAuthInfo</c>.
    /// </remarks>
    public static ShippingGatewayBuilder AddGLS(
        this ShippingGatewayBuilder builder,
        Action<GlsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new GlsOptions();
        configure?.Invoke(options);
        ValidateOptions(options);

        builder.Services.AddSingleton(options);

        builder.Services.AddHttpClient<GlsSoapClient>((_, client) =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = options.Timeout;
        });

        builder.Services.AddScoped<IShippingClient>(sp =>
            new GlsShippingClient(
                sp.GetRequiredService<GlsSoapClient>(),
                sp.GetRequiredService<GlsOptions>(),
                sp.GetRequiredService<ILogger<GlsShippingClient>>()));

        return builder;
    }

    /// <summary>
    /// Rejestruje adapter kurierski GLS w <see cref="ShippingGatewayBuilder"/>,
    /// odczytując konfigurację z sekcji <see cref="GlsOptions.SectionName"/>.
    /// </summary>
    /// <param name="builder">Budowniczy bramy wysyłkowej.</param>
    /// <param name="configuration">Konfiguracja aplikacji.</param>
    /// <returns>Budowniczy bramy wysyłkowej (fluent API).</returns>
    public static ShippingGatewayBuilder AddGLS(
        this ShippingGatewayBuilder builder,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(GlsOptions.SectionName);
        return builder.AddGLS(opt =>
        {
            var baseUrl = section["BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
                opt.BaseUrl = baseUrl;

            var timeout = section["Timeout"];
            if (!string.IsNullOrWhiteSpace(timeout) && TimeSpan.TryParse(timeout, out var ts))
                opt.Timeout = ts;

            var countryCode = section["CountryCode"];
            if (!string.IsNullOrWhiteSpace(countryCode))
                opt.CountryCode = countryCode;

            // Scenariusz B: domyślne credentials naszego konta (opcjonalne)
            var defaultUsername = section[nameof(GlsOptions.DefaultUsername)];
            if (!string.IsNullOrWhiteSpace(defaultUsername))
                opt.DefaultUsername = defaultUsername;

            var defaultPassword = section[nameof(GlsOptions.DefaultPassword)];
            if (!string.IsNullOrWhiteSpace(defaultPassword))
                opt.DefaultPassword = defaultPassword;
        });
    }

    private static void ValidateOptions(GlsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new InvalidOperationException(
                $"Brak konfiguracji {nameof(GlsOptions.BaseUrl)} dla GLS ADE-Plus API.");

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException(
                $"Nieprawidłowy URL '{options.BaseUrl}' w konfiguracji {nameof(GlsOptions.BaseUrl)}.");

        if (options.Timeout <= TimeSpan.Zero)
            throw new InvalidOperationException(
                $"Wartość {nameof(GlsOptions.Timeout)} musi być większa od zera.");
    }
}
