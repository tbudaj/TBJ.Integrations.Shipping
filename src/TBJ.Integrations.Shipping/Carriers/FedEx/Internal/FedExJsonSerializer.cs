using System.Text.Json;
using System.Text.Json.Serialization;

namespace TBJ.Integrations.Shipping.Carriers.FedEx.Internal;

/// <summary>
/// Wewnętrzny serializer JSON dla FedEx REST API.
/// Używa konwencji camelCase i ignoruje wartości null.
/// </summary>
internal static class FedExJsonSerializer
{
    /// <summary>
    /// Domyślne opcje serializacji dla FedEx REST API.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
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
