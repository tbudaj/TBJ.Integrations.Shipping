using TBJ.Integrations.Shipping.Carriers.FedEx;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Carriers.FedEx.Configuration;
using TBJ.Integrations.Shipping.Carriers.FedEx.Internal;
using TBJ.Integrations.Shipping.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping;

/// <summary>
/// Rozszerzenia rejestracji adaptera FedEx w bramie wysyłkowej.
/// </summary>
public static class FedExExtensions
{
    /// <summary>
    /// Rejestruje adapter kurierski FedEx w <see cref="ShippingGatewayBuilder"/>.
    /// </summary>
    /// <param name="builder">Budowniczy bramy wysyłkowej.</param>
    /// <param name="configure">Akcja konfiguracji opcji infrastrukturalnych FedEx (BaseUrl, TokenUrl, Timeout, DefaultServiceType).</param>
    /// <returns>Budowniczy bramy wysyłkowej (fluent API).</returns>
    /// <remarks>
    /// Obsługuje dwa scenariusze uwierzytelniania:
    /// <list type="bullet">
    /// <item><description>
    /// <b>Scenariusz A (konto tenanta):</b> przekaż <c>FedExAuthInfo</c> w każdym żądaniu.
    /// Token OAuth2 jest cache'owany per ClientId w <c>FedExTokenCache</c>.
    /// </description></item>
    /// <item><description>
    /// <b>Scenariusz B (nasze konto):</b> ustaw <c>DefaultClientId</c>, <c>DefaultClientSecret</c>
    /// i <c>DefaultAccountNumber</c> w opcjach — używane automatycznie gdy żądanie nie zawiera <c>FedExAuthInfo</c>.
    /// </description></item>
    /// </list>
    /// </remarks>
    public static ShippingGatewayBuilder AddFedEx(
        this ShippingGatewayBuilder builder,
        Action<FedExOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new FedExOptions();
        configure?.Invoke(options);
        ValidateOptions(options);

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<FedExTokenCache>();

        builder.Services.AddHttpClient<FedExHttpClient>((_, client) =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        builder.Services.AddScoped<IShippingClient>(sp =>
            new FedExShippingClient(
                sp.GetRequiredService<FedExHttpClient>(),
                sp.GetRequiredService<FedExOptions>(),
                sp.GetRequiredService<ILogger<FedExShippingClient>>()));

        return builder;
    }

    /// <summary>
    /// Rejestruje adapter kurierski FedEx w <see cref="ShippingGatewayBuilder"/>,
    /// odczytując konfigurację z sekcji <see cref="FedExOptions.SectionName"/>.
    /// </summary>
    /// <param name="builder">Budowniczy bramy wysyłkowej.</param>
    /// <param name="configuration">Konfiguracja aplikacji.</param>
    /// <returns>Budowniczy bramy wysyłkowej (fluent API).</returns>
    public static ShippingGatewayBuilder AddFedEx(
        this ShippingGatewayBuilder builder,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(FedExOptions.SectionName);
        return builder.AddFedEx(opt =>
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

            var defaultServiceType = section["DefaultServiceType"];
            if (!string.IsNullOrWhiteSpace(defaultServiceType))
                opt.DefaultServiceType = defaultServiceType;

            // Scenariusz B: domyślne credentials naszego konta (opcjonalne)
            var defaultClientId = section[nameof(FedExOptions.DefaultClientId)];
            if (!string.IsNullOrWhiteSpace(defaultClientId))
                opt.DefaultClientId = defaultClientId;

            var defaultClientSecret = section[nameof(FedExOptions.DefaultClientSecret)];
            if (!string.IsNullOrWhiteSpace(defaultClientSecret))
                opt.DefaultClientSecret = defaultClientSecret;

            var defaultAccountNumber = section[nameof(FedExOptions.DefaultAccountNumber)];
            if (!string.IsNullOrWhiteSpace(defaultAccountNumber))
                opt.DefaultAccountNumber = defaultAccountNumber;
        });
    }

    private static void ValidateOptions(FedExOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new InvalidOperationException(
                $"Brak konfiguracji {nameof(FedExOptions.BaseUrl)} dla FedEx API.");

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException(
                $"Nieprawidłowy URL '{options.BaseUrl}' w konfiguracji {nameof(FedExOptions.BaseUrl)}.");

        if (!Uri.TryCreate(options.TokenUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException(
                $"Nieprawidłowy URL '{options.TokenUrl}' w konfiguracji {nameof(FedExOptions.TokenUrl)}.");

        if (options.Timeout <= TimeSpan.Zero)
            throw new InvalidOperationException(
                $"Wartość {nameof(FedExOptions.Timeout)} musi być większa od zera.");
    }
}
