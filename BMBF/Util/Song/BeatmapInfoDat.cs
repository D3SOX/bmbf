#nullable disable

using System.Collections.Generic;
using Newtonsoft.Json;

namespace BMBF.Util.Song;

public class BeatmapInfoDat
{
    [JsonProperty("_levelID")]
    public string LevelId { get; set; }

    [JsonProperty("_songName")]
    public string SongName { get; set; }

    [JsonProperty("_songSubName")]
    public string SongSubName { get; set; }

    [JsonProperty("_songAuthorName")]
    public string SongAuthorName { get; set; }

    [JsonProperty("_levelAuthorName")]
    public string LevelAuthorName { get; set; }

    [JsonProperty("_beatsPerMinute")]
    public float BeatsPerMinute { get; set; }

    [JsonProperty("_songTimeOffset")]
    public float SongTimeOffset { get; set; }

    [JsonProperty("_shuffle")]
    public float Shuffle { get; set; }

    [JsonProperty("_shufflePeriod")]
    public float ShufflePeriod { get; set; }

    [JsonProperty("_previewStartTime")]
    public float PreviewStartTime { get; set; }

    [JsonProperty("_previewDuration")]
    public float PreviewDuration { get; set; }

    [JsonProperty("_difficultyBeatmapSets")]
    public List<DifficultyBeatmapSet> DifficultyBeatmapSets { get; private set; } = new List<DifficultyBeatmapSet>();

    [JsonProperty("_songFilename")]
    public string SongFilename { get; set; }

    [JsonProperty("_coverImageFilename")]
    public string CoverImageFilename { get; set; }

    [JsonProperty("_environmentName")]
    public string EnvironmentName { get; set; }

    [JsonProperty("_allDirectionsEnvironmentName")]
    public string AllDirectionsEnvironmentName { get; set; }

    [JsonProperty("_ignore360MovementBeatmaps")]
    public bool Ignore360MovementBeatmaps { get; set; }
}