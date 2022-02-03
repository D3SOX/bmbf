using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BMBF.Backend.Models.BPList;

// ReSharper disable once InconsistentNaming
public class BPSong
{
    public string Hash { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SongName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Key { get; set; }
    
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    [JsonConstructor]
    public BPSong(string hash, string? songName, string? key)
    {
        Hash = hash;
        SongName = songName;
        Key = key;
    }
}
