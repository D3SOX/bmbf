using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace BMBF.Backend.Extensions;

public static class JsonExtensions
{
    private static readonly JsonSerializerOptions CamelCaseSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<T> ReadAsJsonAsync<T>(this Stream stream, JsonSerializerOptions? serializerOptions = null)
    {
        return await JsonSerializer.DeserializeAsync<T>(stream, serializerOptions) ?? throw new NullReferenceException("Deserialized result was null");
    }

    public static Task<T> ReadAsCamelCaseJsonAsync<T>(this Stream stream) =>
        ReadAsJsonAsync<T>(stream, CamelCaseSerializerOptions);

    public static Task WriteAsJsonAsync(this object o, Stream stream, JsonSerializerOptions? serializerOptions = null)
    {
        return JsonSerializer.SerializeAsync(stream, o, serializerOptions);
    }

    public static Task WriteAsCamelCaseJsonAsync(this object o, Stream stream) =>
        WriteAsJsonAsync(o, stream, CamelCaseSerializerOptions);

    public static T ReadAsJson<T>(this Stream stream, JsonSerializerOptions? serializerOptions = null)
    {
        return JsonSerializer.Deserialize<T>(stream, serializerOptions) ?? throw new NullReferenceException("Deserialized result was null");
    }

    public static T ReadAsCamelCaseJson<T>(this Stream stream) =>
        ReadAsJson<T>(stream, CamelCaseSerializerOptions);

    public static void WriteAsJson(this object o, Stream stream, JsonSerializerOptions? serializerOptions = null)
    {
        JsonSerializer.Serialize(stream, o, serializerOptions);
    }

    public static void WriteAsCamelCaseJson(this object o, Stream stream)
    {
        WriteAsJsonAsync(o, stream, CamelCaseSerializerOptions);
    }
}