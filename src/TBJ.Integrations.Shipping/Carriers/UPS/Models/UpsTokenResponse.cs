using System.Text.Json.Serialization;

namespace TBJ.Integrations.Shipping.Carriers.UPS.Models;

/// <summary>
/// Odpowiedź UPS OAuth 2.0 zawierająca token dostępu.
/// </summary>
internal sealed class UpsTokenResponse
{
    /// <summary>Token dostępu Bearer do autoryzacji żądań API.</summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>Czas ważności tokenu w sekundach.</summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
}
