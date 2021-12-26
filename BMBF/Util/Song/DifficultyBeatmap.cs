using Newtonsoft.Json;

namespace BMBF.Util.Song
{
    public class DifficultyBeatmap
    {
        [JsonProperty("_difficulty")]
        public Difficulty Difficulty { get; set; }

        [JsonProperty("_difficultyRank")]
        public int DifficultyRank { get; set; }

        [JsonProperty("_noteJumpMovementSpeed")]
        public float NoteJumpMovementSpeed { get; set; }

        [JsonProperty("_noteJumpStartBeatOffset")]
        public float NoteJumpStartBeatOffset { get; set; }

        [JsonProperty("_beatmapFilename")]
        public string BeatmapFilename { get; set; }
    }
}