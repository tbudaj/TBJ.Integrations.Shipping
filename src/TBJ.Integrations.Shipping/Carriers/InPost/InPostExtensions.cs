using TBJ.Integrations.Shipping.Carriers.InPost;
using TBJ.Integrations.Shipping.Abstractions.Interfaces;
using TBJ.Integrations.Shipping.Carriers.InPost.Configuration;
using TBJ.Integrations.Shipping.Carriers.InPost.Internal;
using TBJ.Integrations.Shipping.Gateway;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TBJ.Integrations.Shipping;

/// <summary>
/// Rozszerzenia DI do rejestracji adapterów InPost w Shipping Gateway.
/// </summary>
public static class InPostExtensions
{
    /// <summary>
    /// Dodaje adapter InPost Paczkomat do Shipping Gateway.
    /// Rejestruje <see cref="InPostLockerShippingClient"/> obsługujący <c>CarrierType.InPostLocker</c>.
    /// </summary>
    /// <param name="builder">Builder bramy wysyłkowej.</param>
    /// <param name="configure">
    /// Akcja konfiguracji opcji InPost (BaseUrl, Timeout, LabelFormat oraz opcjonalne Default* credentials).
    /// </param>
    /// <returns>Builder bramy wysyłkowej (fluent API).</returns>
    /// <remarks>
    /// Obsługuje dwa scenariusze uwierzytelniania:
    /// <list type="bullet">
    /// <item><description>
    /// <b>Scenariusz A (konto tenanta):</b> przekaż <c>InPostAuthInfo</c> w każdym żądaniu.
    /// </description></item>
    /// <item><description>
    /// <b>Scenariusz B (nasze konto):</b> ustaw <c>DefaultAccessToken</c> i <c>DefaultOrganizationId</c>
    /// w opcjach — używane automatycznie gdy żądanie nie zawiera <c>InPostAuthInfo</c>.
    /// </description></item>
    /// </list>
    /// </remarks>
    public static ShippingGatewayBuilder AddInPostLocker(
        this ShippingGatewayBuilder builder,
        Action<InPostOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new InPostOptions();
        configure?.Invoke(options);

        ValidateOptions(options);
        EnsureHttpClientRegistered(builder.Services, options);

        builder.Services.AddScoped<IShippingClient>(sp =>
            new InPostLockerShippingClient(
                sp.GetRequiredService<InPostHttpClient>(),
                sp.GetRequiredService<InPostOptions>(),
                sp.GetRequiredService<ILogger<InPostLockerShippingClient>>()));

        return builder;
    }

    /// <summary>
    /// Dodaje adapter InPost Paczkomat do Shipping Gateway z konfiguracją
    /// z sekcji <see cref="InPostOptions.SectionName"/> w <c>IConfiguration</c>.
    /// </summary>
    /// <param name="builder">Builder bramy wysyłkowej.</param>
    /// <param name="configuration">Konfiguracja aplikacji.</param>
    /// <returns>Builder bramy wysyłkowej (fluent API).</returns>
    public static ShippingGatewayBuilder AddInPostLocker(
        this ShippingGatewayBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        return builder.AddInPostLocker(opt => BindOptions(opt, configuration));
    }

    /// <summary>
    /// Dodaje adapter InPost Kurier do Shipping Gateway.
    /// Rejestruje <see cref="InPostCourierShippingClient"/> obsługujący <c>CarrierType.InPostCourier</c>.
    /// </summary>
    /// <param name="builder">Builder bramy wysyłkowej.</param>
    /// <param name="configure">
    /// Akcja konfiguracji opcji InPost (BaseUrl, Timeout, LabelFormat oraz opcjonalne Default* credentials).
    /// </param>
    /// <returns>Builder bramy wysyłkowej (fluent API).</returns>
    /// <remarks>
    /// Obsługuje dwa scenariusze uwierzytelniania:
    /// <list type="bullet">
    /// <item><description>
    /// <b>Scenariusz A (konto tenanta):</b> przekaż <c>InPostAuthInfo</c> w każdym żądaniu.
    /// </description></item>
    /// <item><description>
    /// <b>Scenariusz B (nasze konto):</b> ustaw <c>DefaultAccessToken</c> i <c>DefaultOrganizationId</c>
    /// w opcjach — używane automatycznie gdy żądanie nie zawiera <c>InPostAuthInfo</c>.
    /// </description></item>
    /// </list>
    /// </remarks>
    public static ShippingGatewayBuilder AddInPostCourier(
        this ShippingGatewayBuilder builder,
        Action<InPostOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new InPostOptions();
        configure?.Invoke(options);

        ValidateOptions(options);
        EnsureHttpClientRegistered(builder.Services, options);

        builder.Services.AddScoped<IShippingClient>(sp =>
            new InPostCourierShippingClient(
                sp.GetRequiredService<InPostHttpClient>(),
                sp.GetRequiredService<InPostOptions>(),
                sp.GetRequiredService<ILogger<InPostCourierShippingClient>>()));

        return builder;
    }

    /// <summary>
    /// Dodaje adapter InPost Kurier do Shipping Gateway z konfiguracją
    /// z sekcji <see cref="InPostOptions.SectionName"/> w <c>IConfiguration</c>.
    /// </summary>
    /// <param name="builder">Builder bramy wysyłkowej.</param>
    /// <param name="configuration">Konfiguracja aplikacji.</param>
    /// <returns>Builder bramy wysyłkowej (fluent API).</returns>
    public static ShippingGatewayBuilder AddInPostCourier(
        this ShippingGatewayBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        return builder.AddInPostCourier(opt => BindOptions(opt, configuration));
    }

    /// <summary>
    /// Rejestruje <see cref="InPostHttpClient"/> i <see cref="InPostOptions"/> w DI,
    /// jeśli jeszcze nie zostały zarejestrowane (idempotentne — bezpieczne przy rejestracji
    /// obu adapterów jednocześnie, bo współdzielą ten sam klient HTTP i opcje).
    /// </summary>
    private static void EnsureHttpClientRegistered(IServiceCollection services, InPostOptions options)
    {
        // Sprawdzamy, czy InPostOptions już zostało zarejestrowane (np. przy wywołaniu AddInPostLocker + AddInPostCourier).
        // Oba adaptery współdzielą ten sam InPostHttpClient i InPostOptions.
        if (services.Any(d => d.ServiceType == typeof(InPostOptions)))
            return;

        services.AddSingleton(options);

        services.AddHttpClient<InPostHttpClient>((_, client) =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            // Authorization header ustawiany per-żądanie w InPostHttpClient na podstawie InPostAuthInfo
        });
    }

    /// <summary>
    /// Wiąże opcje InPost z sekcją konfiguracji <c>IConfiguration</c>.
    /// </summary>
    private static void BindOptions(InPostOptions opt, IConfiguration configuration)
    {
        var section = configuration.GetSection(InPostOptions.SectionName);

        var baseUrl = section[nameof(InPostOptions.BaseUrl)];
        if (!string.IsNullOrWhiteSpace(baseUrl))
            opt.BaseUrl = baseUrl;

        var timeout = section[nameof(InPostOptions.Timeout)];
        if (!string.IsNullOrWhiteSpace(timeout) && TimeSpan.TryParse(timeout, out var ts))
            opt.Timeout = ts;

        var labelFormat = section[nameof(InPostOptions.LabelFormat)];
        if (!string.IsNullOrWhiteSpace(labelFormat))
            opt.LabelFormat = labelFormat;

        // Scenariusz B: domyślne credentials naszego konta (opcjonalne)
        var defaultAccessToken = section[nameof(InPostOptions.DefaultAccessToken)];
        if (!string.IsNullOrWhiteSpace(defaultAccessToken))
            opt.DefaultAccessToken = defaultAccessToken;

        var defaultOrganizationId = section[nameof(InPostOptions.DefaultOrganizationId)];
        if (!string.IsNullOrWhiteSpace(defaultOrganizationId))
            opt.DefaultOrganizationId = defaultOrganizationId;
    }

    private static void ValidateOptions(InPostOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new InvalidOperationException(
                $"Brak konfiguracji {nameof(InPostOptions.BaseUrl)} dla InPost ShipX API.");

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException(
                $"Nieprawidłowy URL '{options.BaseUrl}' w konfiguracji {nameof(InPostOptions.BaseUrl)}.");

        if (options.Timeout <= TimeSpan.Zero)
            throw new InvalidOperationException(
                $"Wartość {nameof(InPostOptions.Timeout)} musi być większa od zera.");
    }
}
