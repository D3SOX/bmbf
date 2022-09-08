import { HostInfo } from '../types/info';
import { API_ROOT } from './base';
import { proxy } from 'valtio';

export const infoStore = proxy<{ hostInfo: HostInfo | null }>({
  hostInfo: null,
});

export async function fetchHostInfo(): Promise<void> {
  const data = await fetch(`${API_ROOT}/host`);
  if (data.ok) {
    infoStore.hostInfo = await data.json();
  } else {
    infoStore.hostInfo = null;
  }
}
