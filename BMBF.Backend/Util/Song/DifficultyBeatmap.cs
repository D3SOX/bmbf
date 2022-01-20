using System.Text.Json.Serialization;

#nullable disable

namespace BMBF.Backend.Util.Song;

public class DifficultyBeatmap
{
    [JsonPropertyName("_difficulty")]
    public Difficulty Difficulty { get; set; }

    [JsonPropertyName("_difficultyRank")]
    public int DifficultyRank { get; set; }

    [JsonPropertyName("_noteJumpMovementSpeed")]
    public float NoteJumpMovementSpeed { get; set; }

    [JsonPropertyName("_noteJumpStartBeatOffset")]
    public float NoteJumpStartBeatOffset { get; set; }

    [JsonPropertyName("_beatmapFilename")]
    public string BeatmapFilename { get; set; }
}