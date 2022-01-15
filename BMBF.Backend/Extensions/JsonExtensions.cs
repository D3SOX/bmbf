using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BMBF.Backend.Extensions;

public static class JsonExtensions
{
    private static readonly JsonSerializer CamelCaseJsonSerializer = new JsonSerializer
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    private static readonly JsonSerializer DefaultJsonSerializer = new JsonSerializer();
        
    public static T ReadAsJson<T>(this Stream stream, JsonSerializer? jsonSerializer = null)
    {
        using var reader = new StreamReader(stream);
        using var jsonReader = new JsonTextReader(reader);
        return (jsonSerializer ?? DefaultJsonSerializer).Deserialize<T>(jsonReader) ?? throw new NullReferenceException("Deserialized result was null");
    }

    public static T ReadAsCamelCaseJson<T>(this Stream stream) => 
        ReadAsJson<T>(stream, CamelCaseJsonSerializer);

    public static void WriteAsJson(this object o, Stream stream, JsonSerializer? jsonSerializer = null)
    {
        using var writer = new StreamWriter(stream);
        using var jsonWriter = new JsonTextWriter(writer);
        (jsonSerializer ?? DefaultJsonSerializer).Serialize(jsonWriter, o);
    }

    public static void WriteAsCamelCaseJson(this object o, Stream stream) =>
        WriteAsJson(o, stream, CamelCaseJsonSerializer);
}