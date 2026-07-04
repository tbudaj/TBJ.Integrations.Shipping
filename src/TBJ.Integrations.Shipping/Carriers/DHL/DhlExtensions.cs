using TBJ.Integrations.Shipping.Carriers.DHL;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Carriers.DHL.Configuration;
using TBJ.Integrations.Shipping.Carriers.DHL.Internal;
using TBJ.Integrations.Shipping.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping;

/// <summary>
/// Rozszerzenia rejestracji adaptera DHL w bramie wysyłkowej.
/// </summary>
public static class DhlExtensions
{
    /// <summary>
    /// Rejestruje adapter kurierski DHL w <see cref="ShippingGatewayBuilder"/>.
    /// </summary>
    /// <param name="builder">Budowniczy bramy wysyłkowej.</param>
    /// <param name="configure">Akcja konfiguracji opcji infrastrukturalnych DHL (BaseUrl, Timeout, DefaultServiceType).</param>
    /// <returns>Budowniczy bramy wysyłkowej (fluent API).</returns>
    /// <remarks>
    /// <b>Scenariusz A (wielotenantowy):</b> dane uwierzytelniające (Login, Password) są specyficzne
    /// dla tenanta i przekazywane per-żądanie przez <c>DhlAuthInfo</c> w <see cref="CreateShipmentRequest"/>.
    /// <b>Scenariusz B (single-tenant):</b> ustaw <see cref="DhlOptions.DefaultLogin"/>
    /// i <see cref="DhlOptions.DefaultPassword"/> — używane gdy żądanie nie zawiera <c>DhlAuthInfo</c>.
    /// </remarks>
    public static ShippingGatewayBuilder AddDHL(
        this ShippingGatewayBuilder builder,
        Action<DhlOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new DhlOptions();
        configure?.Invoke(options);
        ValidateOptions(options);

        builder.Services.AddSingleton(options);

        builder.Services.AddHttpClient<DhlSoapClient>((_, client) =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = options.Timeout;
        });

        builder.Services.AddScoped<IShippingClient>(sp =>
            new DhlShippingClient(
                sp.GetRequiredService<DhlSoapClient>(),
                sp.GetRequiredService<DhlOptions>(),
                sp.GetRequiredService<ILogger<DhlShippingClient>>()));

        return builder;
    }

    /// <summary>
    /// Rejestruje adapter kurierski DHL w <see cref="ShippingGatewayBuilder"/>,
    /// odczytując konfigurację z sekcji <see cref="DhlOptions.SectionName"/>.
    /// </summary>
    /// <param name="builder">Budowniczy bramy wysyłkowej.</param>
    /// <param name="configuration">Konfiguracja aplikacji.</param>
    /// <returns>Budowniczy bramy wysyłkowej (fluent API).</returns>
    public static ShippingGatewayBuilder AddDHL(
        this ShippingGatewayBuilder builder,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(DhlOptions.SectionName);
        return builder.AddDHL(opt =>
        {
            var baseUrl = section["BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
                opt.BaseUrl = baseUrl;

            var timeout = section["Timeout"];
            if (!string.IsNullOrWhiteSpace(timeout) && TimeSpan.TryParse(timeout, out var ts))
                opt.Timeout = ts;

            var defaultServiceType = section["DefaultServiceType"];
            if (!string.IsNullOrWhiteSpace(defaultServiceType))
                opt.DefaultServiceType = defaultServiceType;

            // Scenariusz B: domyślne credentials naszego konta (opcjonalne)
            var defaultLogin = section[nameof(DhlOptions.DefaultLogin)];
            if (!string.IsNullOrWhiteSpace(defaultLogin))
                opt.DefaultLogin = defaultLogin;

            var defaultPassword = section[nameof(DhlOptions.DefaultPassword)];
            if (!string.IsNullOrWhiteSpace(defaultPassword))
                opt.DefaultPassword = defaultPassword;
        });
    }

    private static void ValidateOptions(DhlOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new InvalidOperationException(
                $"Brak konfiguracji {nameof(DhlOptions.BaseUrl)} dla DHL24 WebAPI2.");

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException(
                $"Nieprawidłowy URL '{options.BaseUrl}' w konfiguracji {nameof(DhlOptions.BaseUrl)}.");

        if (options.Timeout <= TimeSpan.Zero)
            throw new InvalidOperationException(
                $"Wartość {nameof(DhlOptions.Timeout)} musi być większa od zera.");
    }
}
