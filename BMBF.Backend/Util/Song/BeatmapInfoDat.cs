#nullable disable

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BMBF.Backend.Util.Song;

public class BeatmapInfoDat
{
    [JsonPropertyName("_levelID")]
    public string LevelId { get; set; }

    [JsonPropertyName("_songName")]
    public string SongName { get; set; }

    [JsonPropertyName("_songSubName")]
    public string SongSubName { get; set; }

    [JsonPropertyName("_songAuthorName")]
    public string SongAuthorName { get; set; }

    [JsonPropertyName("_levelAuthorName")]
    public string LevelAuthorName { get; set; }

    [JsonPropertyName("_beatsPerMinute")]
    public float BeatsPerMinute { get; set; }

    [JsonPropertyName("_songTimeOffset")]
    public float SongTimeOffset { get; set; }

    [JsonPropertyName("_shuffle")]
    public float Shuffle { get; set; }

    [JsonPropertyName("_shufflePeriod")]
    public float ShufflePeriod { get; set; }

    [JsonPropertyName("_previewStartTime")]
    public float PreviewStartTime { get; set; }

    [JsonPropertyName("_previewDuration")]
    public float PreviewDuration { get; set; }

    [JsonPropertyName("_difficultyBeatmapSets")]
    public List<DifficultyBeatmapSet> DifficultyBeatmapSets { get; set; }

    [JsonPropertyName("_songFilename")]
    public string SongFilename { get; set; }

    [JsonPropertyName("_coverImageFilename")]
    public string CoverImageFilename { get; set; }

    [JsonPropertyName("_environmentName")]
    public string EnvironmentName { get; set; }

    [JsonPropertyName("_allDirectionsEnvironmentName")]
    public string AllDirectionsEnvironmentName { get; set; }

    [JsonPropertyName("_ignore360MovementBeatmaps")]
    public bool Ignore360MovementBeatmaps { get; set; }
}