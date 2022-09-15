import { CoreModInstallResult, CoreModResultType, Mod } from '../types/mod';
import { backendRequest, sendErrorNotification } from './base';
import { proxy } from 'valtio';

export const modsStore = proxy<{ mods: Mod[] }>({ mods: [] });

export async function fetchMods(): Promise<void> {
  const data = await backendRequest(`mods`);
  if (data.ok) {
    modsStore.mods = await data.json();
  }
}

export async function uninstallMod(mod: Pick<Mod, 'id'>): Promise<void> {
  const data = await backendRequest(
    `mods/uninstall/${mod.id}`,
    {
      method: 'POST',
    },
    [400]
  );
  if (data.status === 400) {
    const message = await data.text();
    sendErrorNotification(`Failed to uninstall ${mod.id}: ${message}`);
  }
}

export async function installMod(mod: Pick<Mod, 'id'>): Promise<void> {
  const data = await backendRequest(
    `mods/install/${mod.id}`,
    {
      method: 'POST',
    },
    [400]
  );

  if (data.status === 400) {
    const message = await data.text();
    sendErrorNotification(`Failed to install ${mod.id}: ${message}`);
  }
}

export async function unloadMod(mod: Pick<Mod, 'id'>): Promise<void> {
  await backendRequest(`mods/unload/${mod.id}`, {
    method: 'POST',
  });
}

export async function installCore(): Promise<void> {
  const data = await backendRequest('mods/installcore', {
    method: 'POST',
  });
  if (data.ok) {
    const result: CoreModInstallResult = await data.json();
    switch (result.resultType) {
      case CoreModResultType.BeatSaberNotInstalled:
        sendErrorNotification('Beat Saber is not installed');
        break;
      case CoreModResultType.FailedToFetch:
        sendErrorNotification('Failed to fetch');
        break;
      case CoreModResultType.NoneAvailableForVersion:
        sendErrorNotification('No core mods available for this version');
        break;
      case CoreModResultType.NoneBuiltInForVersion:
        sendErrorNotification('No built-in core mods available for this version');
        break;
      default:
        break;
    }
  }
}
