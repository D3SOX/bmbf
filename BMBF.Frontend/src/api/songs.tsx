import { Song } from '../types/song';
import { API_ROOT, backendRequest, sendErrorNotification } from './base';
import { proxy } from 'valtio';

export const songsStore = proxy<{ songs: Song[] }>({ songs: [] });

export async function fetchSongs(): Promise<void> {
  const data = await backendRequest(`songs`);
  if (data.ok) {
    songsStore.songs = await data.json();
  }
}

export async function deleteSong(song: Pick<Song, 'hash'>): Promise<void> {
  const data = await backendRequest(`songs/delete/${song.hash}`, {
    method: 'DELETE',
  });
}
