using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Xabbo.Scripter.Services;

public class DotNetJsonSerializer : IJsonSerializer
{
    private readonly JsonSerializerOptions
       _jsonSerializerOptions,
       _jsonSerializerOptionsIndented;

    public DotNetJsonSerializer()
    {
        _jsonSerializerOptions = new JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _jsonSerializerOptionsIndented = new JsonSerializerOptions(_jsonSerializerOptions)
        {
            WriteIndented = true
        };
    }

    public T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions);
    }

    public string Serialize<T>(T value, bool indented = true)
    {
        return JsonSerializer.Serialize(value, indented ? _jsonSerializerOptionsIndented : _jsonSerializerOptions);
    }
}
