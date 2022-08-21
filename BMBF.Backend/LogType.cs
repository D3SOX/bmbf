using System.Text.Json.Serialization;

namespace BMBF.Backend;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LogType
{
    ModInstallation,
    Setup
}
