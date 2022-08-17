import { API_ROOT, sendErrorNotification } from './base';
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
  const data = await fetch(`${API_ROOT}/setup/status`);
  if (data.ok) {
    setupStore.setupStatus = await data.json();
  } else {
    setupStore.setupStatus = null;
  }
}

export async function fetchModdableVersions(): Promise<void> {
  const data = await fetch(`${API_ROOT}/setup/moddableversions`);
  if (data.ok) {
    setupStore.moddableVersions = await data.json();
  } else {
    sendErrorNotification(await data.text());
  }
}

export async function begin(): Promise<void> {
  if (!setupStore.setupStatus) {
    setupStore.loadingStep = 0;
    await fetch(`${API_ROOT}/setup/begin`, {
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
    await fetch(`${API_ROOT}/setup/downgrade`, {
      method: 'POST',
      body: `"${version}"`,
    });
  }
}

export async function patch(): Promise<void> {
  if (setupStore.setupStatus) {
    await fetch(`${API_ROOT}/setup/patch`, {
      method: 'POST',
    });
  }
}

export async function triggerUninstall(): Promise<void> {
  if (setupStore.setupStatus) {
    await fetch(`${API_ROOT}/setup/triggeruninstall`, {
      method: 'POST',
    });
  }
}

export async function triggerInstall(): Promise<void> {
  if (setupStore.setupStatus) {
    await fetch(`${API_ROOT}/setup/triggerinstall`, {
      method: 'POST',
    });
  }
}

export async function finalize(): Promise<void> {
  if (setupStore.setupStatus) {
    await fetch(`${API_ROOT}/setup/finalize`, {
      method: 'POST',
    });
  }
}

export async function quit(): Promise<void> {
  if (setupStore.setupStatus) {
    const data = await fetch(`${API_ROOT}/setup/quit`, {
      method: 'POST',
    });
    if (data.ok) {
      setupStore.setupStatus = null;
      // TODO: do I need to reset beatSaber installationInfo here too?
    }
  }
}
