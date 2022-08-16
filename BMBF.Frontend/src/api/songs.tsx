import { Song } from '../types/song';
import { API_ROOT, sendErrorNotification } from './base';
import { proxy } from 'valtio';

export const songsStore = proxy<{ songs: Song[] }>({ songs: [] });

export async function fetchSongs(): Promise<void> {
  const data = await fetch(`${API_ROOT}/songs`);
  if (data.ok) {
    songsStore.songs = await data.json();
  } else {
    sendErrorNotification(await data.text());
  }
}

export async function deleteSong(song: Pick<Song, 'hash'>): Promise<void> {
  const data = await fetch(`${API_ROOT}/songs/delete/${song.hash}`, {
    method: 'DELETE',
  });
  // this can be removed when the websocket is implemented
  if (data.ok) {
    const index = songsStore.songs.findIndex(s => s.hash === song.hash);
    songsStore.songs.splice(index, 1);
  }
}
