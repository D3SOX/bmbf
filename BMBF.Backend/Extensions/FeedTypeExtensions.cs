using System;
using BMBF.Backend.Models;

namespace BMBF.Backend.Extensions;

public static class FeedTypeExtensions
{
    public static string GetDisplayName(this FeedType type) => type switch
    {
        FeedType.ScoreSaberTrending => "ScoreSaber Trending",
        FeedType.ScoreSaberLatestRanked => "Latest Ranked",
        FeedType.ScoreSaberTopRanked => "Top Ranked",
        FeedType.BeatSaverLatest => "Latest",
        FeedType.BeastSaberCurated => "BeastSaber Curated",
        FeedType.BeastSaberBookmarks => "BeastSaber Bookmarks",
        FeedType.ScoreSaberTopPlayed => "ScoreSaber Top Played",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static string GetFeedId(this FeedType type) => type switch
    {
        FeedType.ScoreSaberTrending => "ScoreSaber.Trending",
        FeedType.ScoreSaberLatestRanked => "ScoreSaber.LatestRanked",
        FeedType.ScoreSaberTopRanked => "ScoreSaber.TopRanked",
        FeedType.BeatSaverLatest => "BeatSaver.Latest",
        FeedType.BeastSaberCurated => "BeastSaber.CuratorRecommended",
        FeedType.ScoreSaberTopPlayed => "ScoreSaber.TopPlayed",
        FeedType.BeastSaberBookmarks => "BeastSaber.Bookmarks",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
