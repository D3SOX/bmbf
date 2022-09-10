import { proxy } from 'valtio';
import { Progress } from '../types/progress';
import { API_ROOT, backendRequest } from './base';

export const progressStore = proxy<{ progress: Progress[] }>({
  progress: [],
});

export async function fetchProgress(): Promise<void> {
  const data = await backendRequest(`progress`);
  if (data.ok) {
    progressStore.progress = await data.json();
  }
}
