import { InstallationInfo } from '../types/beatsaber';
import { API_ROOT } from './base';
import { proxy, useSnapshot } from 'valtio';
import { useMemo } from 'react';
import { setupStore } from './setup';

export const beatSaberStore = proxy<{ installationInfo: InstallationInfo | null }>({
  installationInfo: null,
});

export async function fetchInstallationInfo(): Promise<void> {
  const data = await fetch(`${API_ROOT}/beatsaber/install`);
  if (data.ok) {
    beatSaberStore.installationInfo = await data.json();
  } else {
    beatSaberStore.installationInfo = null;
  }
}

export async function launchBeatSaber(): Promise<void> {
  await fetch(`${API_ROOT}/beatsaber/launch`, { method: 'POST' });
}

export function useNeedsSetup() {
  const { installationInfo } = useSnapshot(beatSaberStore);
  const { setupStatus } = useSnapshot(setupStore);

  const needsSetup = useMemo(() => {
    return installationInfo === null || installationInfo.modTag === null || setupStatus !== null;
  }, [installationInfo, setupStatus]);

  return needsSetup;
}
