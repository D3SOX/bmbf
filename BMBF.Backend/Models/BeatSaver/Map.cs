using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BMBF.Backend.Models.BeatSaver;

/// <summary>
/// Structure of the BeatSaver response for a map.
/// This is incomplete.
/// </summary>
public class Map
{
    public Map(List<MapVersion> versions)
    {
        Versions = versions;
    }

    [JsonPropertyName("versions")]
    public List<MapVersion> Versions { get; set; }
}