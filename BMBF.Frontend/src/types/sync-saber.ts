export const enum FeedType {
  ScoreSaberTrending = 'ScoreSaberTrending',
  ScoreSaberLatestRanked = 'ScoreSaberLatestRanked',
  ScoreSaberTopRanked = 'ScoreSaberTopRanked',
  ScoreSaberTopPlayed = 'ScoreSaberTopPlayed',
  BeatSaverLatest = 'BeatSaverLatest',
  BeastSaberCurated = 'BeastSaberCurated',
  BeastSaberBookmarks = 'BeastSaberBookmarks',
}

export interface FeedSettings {
  songsToSync: number;
  enabled: boolean;
}

export interface SyncSaberConfig extends Record<FeedType, FeedSettings> {
  beastSaberUsername: string | null;
}
