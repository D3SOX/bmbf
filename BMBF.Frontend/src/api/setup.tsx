import { API_ROOT, sendErrorNotification } from './base';
import { SetupStage, SetupStatus, Versions } from '../types/setup';
import { proxy } from 'valtio';

export const setupStore = proxy<{ setupStatus: SetupStatus | null; moddableVersions: Versions }>({
  setupStatus: null,
  moddableVersions: [],
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
    const data = await fetch(`${API_ROOT}/setup/begin`, {
      method: 'POST',
    });
    // this can be removed when the websocket is implemented
    if (data.ok) {
      await fetchSetupStatus();
    }
  }
}

export function needsDowngrade(): boolean {
  if (setupStore.setupStatus) {
    if (setupStore.setupStatus.stage === SetupStage.Downgrading) {
      return setupStore.moddableVersions.includes(setupStore.setupStatus.currentBeatSaberVersion);
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
    // this can be removed when the websocket is implemented
    await fetchSetupStatus();
  }
}

export async function patch(): Promise<void> {
  if (setupStore.setupStatus) {
    await fetch(`${API_ROOT}/setup/patch`, {
      method: 'POST',
    });
    // this can be removed when the websocket is implemented
    await fetchSetupStatus();
  }
}

export async function triggerUninstall(): Promise<void> {
  if (setupStore.setupStatus) {
    await fetch(`${API_ROOT}/setup/triggeruninstall`, {
      method: 'POST',
    });
    // this can be removed when the websocket is implemented
    await fetchSetupStatus();
  }
}

export async function triggerInstall(): Promise<void> {
  if (setupStore.setupStatus) {
    await fetch(`${API_ROOT}/setup/triggerinstall`, {
      method: 'POST',
    });
    // this can be removed when the websocket is implemented
    await fetchSetupStatus();
  }
}

export async function finalize(): Promise<void> {
  if (setupStore.setupStatus) {
    await fetch(`${API_ROOT}/setup/finalize`, {
      method: 'POST',
    });
    // this can be removed when the websocket is implemented
    await fetchSetupStatus();
  }
}
