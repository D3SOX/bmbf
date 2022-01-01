using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BMBF.Extensions
{
    public static class JsonExtensions
    {
        private static JsonSerializer JsonSerializer = new JsonSerializer
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        
        public static T ReadAsCamelCaseJson<T>(this Stream stream)
        {
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            return JsonSerializer.Deserialize<T>(jsonReader) ?? throw new NullReferenceException("Deserialized result was null");
        }

        public static void WriteAsCamelCaseJson(this object o, Stream stream)
        {
            using var writer = new StreamWriter(stream);
            using var jsonWriter = new JsonTextWriter(writer);
            JsonSerializer.Serialize(jsonWriter, o);
        }
    }
}