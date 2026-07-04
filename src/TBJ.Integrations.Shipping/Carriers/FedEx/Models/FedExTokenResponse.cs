using System.Text.Json.Serialization;

namespace TBJ.Integrations.Shipping.Carriers.FedEx.Models;

/// <summary>
/// Odpowiedź FedEx OAuth 2.0 zawierająca token dostępu.
/// </summary>
internal sealed class FedExTokenResponse
{
    /// <summary>Token dostępu Bearer do autoryzacji żądań API.</summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>Czas ważności tokenu w sekundach.</summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
}
