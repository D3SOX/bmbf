#nullable disable

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BMBF.Backend.Util.Song;

public class DifficultyBeatmapSet
{
    [JsonPropertyName("_difficultyBeatmaps")]
    public List<DifficultyBeatmap> DifficultyBeatmaps { get; private set; } = new List<DifficultyBeatmap>();

    [JsonPropertyName("_beatmapCharacteristicName")]
    public string BeatmapCharacteristicName { get; set; }
}