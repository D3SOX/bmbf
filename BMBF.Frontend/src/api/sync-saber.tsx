import { backendRequest } from './base';
import { FeedType, SyncSaberConfig } from '../types/sync-saber';
import { proxy } from 'valtio';

export const syncSaberStore = proxy<{ syncSaberConfig: SyncSaberConfig | null }>({
  syncSaberConfig: null,
});

export async function fetchSyncSaberConfig(): Promise<void> {
  const data = await backendRequest('syncsaber/config');
  if (data.ok) {
    syncSaberStore.syncSaberConfig = await data.json();
  }
}

export function setBeastSaberUsername(beastSaberUsername: string) {
  if (syncSaberStore.syncSaberConfig) {
    syncSaberStore.syncSaberConfig.beastSaberUsername = beastSaberUsername;
  }
}

export function setFeedSongsToSync(feedType: FeedType, songsToSync: number) {
  if (syncSaberStore.syncSaberConfig) {
    syncSaberStore.syncSaberConfig.feeds[feedType].songsToSync = songsToSync;
  }
}
export function setFeedEnabled(feedType: FeedType, enabled: boolean) {
  if (syncSaberStore.syncSaberConfig) {
    syncSaberStore.syncSaberConfig.feeds[feedType].enabled = enabled;
  }
}

export async function saveSyncSaberConfig(): Promise<void> {
  await backendRequest('syncsaber/config', {
    method: 'PUT',
    body: JSON.stringify(syncSaberStore.syncSaberConfig),
  });
}
