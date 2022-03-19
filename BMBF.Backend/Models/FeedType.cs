using System.Text.Json.Serialization;

namespace BMBF.Backend.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeedType
{
    ScoreSaberTrending,
    ScoreSaberLatestRanked,
    ScoreSaberTopRanked,
    ScoreSaberTopPlayed,
    BeatSaverLatest,
    BeastSaberCurated,
    BeastSaberBookmarks
}
