using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BMBF.Backend.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum FileImportResultType
{
    Song,
    Mod,
    FileCopy,
    Config,
    Playlist,
    Failed
}