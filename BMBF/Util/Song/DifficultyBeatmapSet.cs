#nullable disable

using System.Collections.Generic;
using Newtonsoft.Json;

namespace BMBF.Util.Song
{
    public class DifficultyBeatmapSet
    {
        [JsonProperty("_difficultyBeatmaps")]
        public List<DifficultyBeatmap> DifficultyBeatmaps { get; private set; } = new List<DifficultyBeatmap>();

        [JsonProperty("_beatmapCharacteristicName")]
        public string BeatmapCharacteristicName { get; set; }
    }
}