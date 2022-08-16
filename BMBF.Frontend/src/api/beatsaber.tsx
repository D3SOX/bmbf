import { InstallationInfo } from '../types/beatsaber';
import { API_ROOT } from './base';
import { proxy } from 'valtio';

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
