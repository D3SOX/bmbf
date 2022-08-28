import { API_ROOT, backendRequest } from './base';
import { SetupStage, SetupStatus, Versions } from '../types/setup';
import { proxy } from 'valtio';

export const setupStore = proxy<{
  setupStatus: SetupStatus | null;
  moddableVersions: Versions;
  loadingStep: number | null;
}>({
  setupStatus: null,
  moddableVersions: [],
  loadingStep: null,
});

export async function fetchSetupStatus(): Promise<void> {
  const data = await backendRequest(`setup/status`, undefined, [404]);
  if (data.ok) {
    setupStore.setupStatus = await data.json();
  } else {
    setupStore.setupStatus = null;
  }
}

export async function fetchModdableVersions(): Promise<void> {
  const data = await backendRequest(`setup/moddableversions`, undefined);
  if (data.ok) {
    setupStore.moddableVersions = await data.json();
  }
}

export async function begin(): Promise<void> {
  if (!setupStore.setupStatus) {
    setupStore.loadingStep = 0;
    await backendRequest(`setup/begin`, {
      method: 'POST',
    });
  }
}

export function needsDowngrade(): boolean {
  if (setupStore.setupStatus) {
    if (setupStore.setupStatus.stage === SetupStage.Downgrading) {
      return !setupStore.moddableVersions.includes(setupStore.setupStatus.currentBeatSaberVersion);
    }
  }
  return false;
}

export async function downgrade(version: string): Promise<void> {
  if (needsDowngrade()) {
    await backendRequest(`setup/downgrade`, {
      method: 'POST',
      body: `"${version}"`,
    });
  }
}

export async function patch(): Promise<void> {
  if (setupStore.setupStatus) {
    await backendRequest(`setup/patch`, {
      method: 'POST',
    });
  }
}

export async function triggerUninstall(): Promise<void> {
  if (setupStore.setupStatus) {
    await backendRequest(`setup/triggeruninstall`, {
      method: 'POST',
    });
  }
}

export async function triggerInstall(): Promise<void> {
  if (setupStore.setupStatus) {
    await backendRequest(`setup/triggerinstall`, {
      method: 'POST',
    });
  }
}

export async function finalize(): Promise<void> {
  if (setupStore.setupStatus) {
    await backendRequest(`setup/finalize`, {
      method: 'POST',
    });
  }
}

export async function quit(): Promise<void> {
  if (setupStore.setupStatus) {
    await backendRequest(`setup/quit`, {
      method: 'POST',
    });
  }
}
