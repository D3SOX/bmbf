using System.Text.Json.Serialization;

namespace BMBF.Backend.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FileImportResultType
{
    Song,
    Mod,
    FileCopy,
    Config,
    Playlist,
    Failed
}