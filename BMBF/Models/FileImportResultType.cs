using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BMBF.Models
{
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
}