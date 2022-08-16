import { Mod } from '../types/mod';
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
