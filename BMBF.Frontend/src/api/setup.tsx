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

export async function backendRequest(...args: Parameters<typeof fetch>): Promise<Response> {
  try {
    const result = await fetch(...args)

    if (result.ok) return result;

    console.error("Request failed", result)
    sendErrorNotification(`Error while making request, received ${result.status}\n${await result.text()}`)

    return result;
  } catch (e) {
    console.error("Error while fulfilling request", e)
    sendErrorNotification(`Error while making request`)
    throw e;
  }
}

export async function fetchSetupStatus(): Promise<void> {
  const data = await backendRequest(`${API_ROOT}/setup/status`);
  if (data.ok) {
    setupStore.setupStatus = await data.json();
  } else {
    setupStore.setupStatus = null;
  }
}

export async function fetchModdableVersions(): Promise<void> {
  const data = await backendRequest(`${API_ROOT}/setup/moddableversions`);
  if (data.ok) {
    setupStore.moddableVersions = await data.json();
  }
}

export async function begin(): Promise<void> {
  if (!setupStore.setupStatus) {
    setupStore.loadingStep = 0;
    await backendRequest(`${API_ROOT}/setup/begin`, {
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
    await backendRequest(`${API_ROOT}/setup/downgrade`, {
      method: 'POST',
      body: `"${version}"`,
    });
  }
}

export async function patch(): Promise<void> {
  if (setupStore.setupStatus) {
    await backendRequest(`${API_ROOT}/setup/patch`, {
      method: 'POST',
    });
  }
}

export async function triggerUninstall(): Promise<void> {
  if (setupStore.setupStatus) {
    await backendRequest(`${API_ROOT}/setup/triggeruninstall`, {
      method: 'POST',
    });
  }
}

export async function triggerInstall(): Promise<void> {
  if (setupStore.setupStatus) {
    await backendRequest(`${API_ROOT}/setup/triggerinstall`, {
      method: 'POST',
    });
  }
}

export async function finalize(): Promise<void> {
  if (setupStore.setupStatus) {
    await backendRequest(`${API_ROOT}/setup/finalize`, {
      method: 'POST',
    });
  }
}

export async function quit(): Promise<void> {
  if (setupStore.setupStatus) {
    await backendRequest(`${API_ROOT}/setup/quit`, {
      method: 'POST',
    });
  }
}
