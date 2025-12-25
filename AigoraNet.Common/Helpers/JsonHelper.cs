using System.Text.Json;
using System.Text.Json.Serialization;

namespace AigoraNet.Common.Helpers;

public class JsonHelper
{
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
    };

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _options);
    }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, _options);
    }
}
