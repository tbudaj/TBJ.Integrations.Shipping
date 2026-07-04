using System.Text.Json;
using System.Text.Json.Serialization;

namespace TBJ.Integrations.Shipping.Carriers.InPost.Internal;

/// <summary>
/// Wewnętrzny serializer JSON dla InPost ShipX API.
/// Używa konwencji snake_case i ignoruje właściwości o wartości <c>null</c>.
/// </summary>
internal static class InPostJsonSerializer
{
    /// <summary>
    /// Domyślne opcje serializacji dla InPost ShipX API.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    /// <summary>
    /// Deserializuje ciąg JSON do obiektu danego typu.
    /// </summary>
    /// <typeparam name="T">Typ docelowy deserializacji.</typeparam>
    /// <param name="json">Ciąg JSON do deserializacji.</param>
    /// <returns>Zdeserializowany obiekt lub <c>null</c>.</returns>
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    /// <summary>
    /// Serializuje obiekt do ciągu JSON.
    /// </summary>
    /// <typeparam name="T">Typ serializowanego obiektu.</typeparam>
    /// <param name="value">Obiekt do serializacji.</param>
    /// <returns>Ciąg JSON.</returns>
    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }
}
