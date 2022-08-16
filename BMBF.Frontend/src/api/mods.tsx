import { CoreModInstallResult, CoreModResultType, Mod } from '../types/mod';
import { API_ROOT, sendErrorNotification } from './base';
import { proxy } from 'valtio';

export const modsStore = proxy<{ mods: Mod[] }>({ mods: [] });

export async function fetchMods(): Promise<void> {
  const data = await fetch(`${API_ROOT}/mods`);
  if (data.ok) {
    modsStore.mods = await data.json();
  } else {
    sendErrorNotification(await data.text());
  }
}

export async function uninstallMod(mod: Pick<Mod, 'id'>): Promise<void> {
  const data = await fetch(`${API_ROOT}/mods/uninstall/${mod.id}`, {
    method: 'POST',
  });
  // this can be removed when the websocket is implemented
  if (data.ok) {
    const theMod = modsStore.mods.find(m => m.id === mod.id);
    if (theMod) {
      theMod.installed = false;
    }
  }
}

export async function installMod(mod: Pick<Mod, 'id'>): Promise<void> {
  const data = await fetch(`${API_ROOT}/mods/install/${mod.id}`, {
    method: 'POST',
  });
  // this can be removed when the websocket is implemented
  if (data.ok) {
    const theMod = modsStore.mods.find(m => m.id === mod.id);
    if (theMod) {
      theMod.installed = true;
    }
  }
}

export async function installCore(): Promise<void> {
  const data = await fetch(`${API_ROOT}/mods/installcore`, {
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
  } else {
    sendErrorNotification(await data.text());
  }
}
