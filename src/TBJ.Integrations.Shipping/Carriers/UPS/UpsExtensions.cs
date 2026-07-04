using TBJ.Integrations.Shipping.Carriers.UPS;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Carriers.UPS.Configuration;
using TBJ.Integrations.Shipping.Carriers.UPS.Internal;
using TBJ.Integrations.Shipping.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping;

/// <summary>
/// Rozszerzenia rejestracji adaptera UPS w bramie wysyłkowej.
/// </summary>
public static class UpsExtensions
{
    /// <summary>
    /// Rejestruje adapter kurierski UPS w <see cref="ShippingGatewayBuilder"/>.
    /// </summary>
    /// <param name="builder">Budowniczy bramy wysyłkowej.</param>
    /// <param name="configure">Akcja konfiguracji opcji infrastrukturalnych UPS (BaseUrl, TokenUrl, Timeout, DefaultServiceCode).</param>
    /// <returns>Budowniczy bramy wysyłkowej (fluent API).</returns>
    /// <remarks>
    /// <b>Scenariusz A (wielotenantowy):</b> dane uwierzytelniające (ClientId, ClientSecret, AccountNumber)
    /// są specyficzne dla tenanta i przekazywane per-żądanie przez <c>UpsAuthInfo</c>
    /// w <see cref="CreateShipmentRequest"/>. Token OAuth2 jest cache'owany per ClientId
    /// w <see cref="UpsTokenCache"/>.
    /// <b>Scenariusz B (single-tenant):</b> ustaw <see cref="UpsOptions.DefaultClientId"/>,
    /// <see cref="UpsOptions.DefaultClientSecret"/> i <see cref="UpsOptions.DefaultAccountNumber"/>
    /// — używane gdy żądanie nie zawiera <c>UpsAuthInfo</c>.
    /// </remarks>
    public static ShippingGatewayBuilder AddUPS(
        this ShippingGatewayBuilder builder,
        Action<UpsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new UpsOptions();
        configure?.Invoke(options);
        ValidateOptions(options);

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<UpsTokenCache>();

        builder.Services.AddHttpClient<UpsHttpClient>((_, client) =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        builder.Services.AddScoped<IShippingClient>(sp =>
            new UpsShippingClient(
                sp.GetRequiredService<UpsHttpClient>(),
                sp.GetRequiredService<UpsOptions>(),
                sp.GetRequiredService<ILogger<UpsShippingClient>>()));

        return builder;
    }

    /// <summary>
    /// Rejestruje adapter kurierski UPS w <see cref="ShippingGatewayBuilder"/>,
    /// odczytując konfigurację z sekcji <see cref="UpsOptions.SectionName"/>.
    /// </summary>
    /// <param name="builder">Budowniczy bramy wysyłkowej.</param>
    /// <param name="configuration">Konfiguracja aplikacji.</param>
    /// <returns>Budowniczy bramy wysyłkowej (fluent API).</returns>
    public static ShippingGatewayBuilder AddUPS(
        this ShippingGatewayBuilder builder,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(UpsOptions.SectionName);
        return builder.AddUPS(opt =>
        {
            var baseUrl = section["BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
                opt.BaseUrl = baseUrl;

            var tokenUrl = section["TokenUrl"];
            if (!string.IsNullOrWhiteSpace(tokenUrl))
                opt.TokenUrl = tokenUrl;

            var timeout = section["Timeout"];
            if (!string.IsNullOrWhiteSpace(timeout) && TimeSpan.TryParse(timeout, out var ts))
                opt.Timeout = ts;

            var defaultServiceCode = section["DefaultServiceCode"];
            if (!string.IsNullOrWhiteSpace(defaultServiceCode))
                opt.DefaultServiceCode = defaultServiceCode;

            // Scenariusz B: domyślne credentials naszego konta (opcjonalne)
            var defaultClientId = section[nameof(UpsOptions.DefaultClientId)];
            if (!string.IsNullOrWhiteSpace(defaultClientId))
                opt.DefaultClientId = defaultClientId;

            var defaultClientSecret = section[nameof(UpsOptions.DefaultClientSecret)];
            if (!string.IsNullOrWhiteSpace(defaultClientSecret))
                opt.DefaultClientSecret = defaultClientSecret;

            var defaultAccountNumber = section[nameof(UpsOptions.DefaultAccountNumber)];
            if (!string.IsNullOrWhiteSpace(defaultAccountNumber))
                opt.DefaultAccountNumber = defaultAccountNumber;
        });
    }

    private static void ValidateOptions(UpsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new InvalidOperationException(
                $"Brak konfiguracji {nameof(UpsOptions.BaseUrl)} dla UPS API.");

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException(
                $"Nieprawidłowy URL '{options.BaseUrl}' w konfiguracji {nameof(UpsOptions.BaseUrl)}.");

        if (!Uri.TryCreate(options.TokenUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException(
                $"Nieprawidłowy URL '{options.TokenUrl}' w konfiguracji {nameof(UpsOptions.TokenUrl)}.");

        if (options.Timeout <= TimeSpan.Zero)
            throw new InvalidOperationException(
                $"Wartość {nameof(UpsOptions.Timeout)} musi być większa od zera.");
    }
}
