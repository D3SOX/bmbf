import { InstallationInfo } from '../types/beatsaber';
import { backendRequest } from './base';
import { proxy, useSnapshot } from 'valtio';
import { useMemo } from 'react';
import { setupStore } from './setup';

export const beatSaberStore = proxy<{ installationInfo: InstallationInfo | null }>({
  installationInfo: null,
});

export async function fetchInstallationInfo(): Promise<void> {
  const data = await backendRequest('beatsaber/install', undefined, [404]);
  if (data.ok) {
    beatSaberStore.installationInfo = await data.json();
  } else {
    beatSaberStore.installationInfo = null;
  }
}

export async function launchBeatSaber(): Promise<void> {
  await backendRequest('beatsaber/launch', {
    method: 'POST',
  });
}

export function useNeedsSetup() {
  const { installationInfo } = useSnapshot(beatSaberStore);
  const { setupStatus } = useSnapshot(setupStore);

  const needsSetup = useMemo(() => {
    return installationInfo === null || installationInfo.modTag === null || setupStatus !== null;
  }, [installationInfo, setupStatus]);

  return needsSetup;
}
