using System.Text.Json;
using System.Text.Json.Serialization;

namespace BulletinBoard.Api.Storage;

/// <summary>One JSON shape for the API and the file: camelCase, enums as strings, compact.</summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = Configure(new JsonSerializerOptions());

    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.WriteIndented = false;
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
