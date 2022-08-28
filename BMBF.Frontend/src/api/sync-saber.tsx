import { API_ROOT, sendErrorNotification } from './base';
import { SyncSaberConfig } from '../types/sync-saber';
import { proxy } from 'valtio';
import { backendRequest } from './setup';

export const syncSaberStore = proxy<{ syncSaberConfig: Partial<SyncSaberConfig> }>({
  syncSaberConfig: {},
});

export async function fetchSyncSaberConfig(): Promise<void> {
  const data = await backendRequest(`${API_ROOT}/syncsaber/config`);
  if (data.ok) {
    syncSaberStore.syncSaberConfig = await data.json();
  }
}

export async function setSyncSaberConfig(config: Partial<SyncSaberConfig>): Promise<void> {
  const data = await backendRequest(`${API_ROOT}/syncsaber/config`, {
    method: 'PUT',
    body: JSON.stringify(config),
  });
  // this can be removed when the websocket is implemented
  if (data.ok) {
    syncSaberStore.syncSaberConfig = config;
  }
}
