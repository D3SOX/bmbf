import { Song } from '../types/song';
import { API_ROOT, sendErrorNotification } from './base';
import { proxy } from 'valtio';
import { ImportResponse } from '../types/import';

export const songsStore = proxy<{ songs: Song[] }>({ songs: [] });

export async function fetchSongs(): Promise<void> {
  const data = await fetch(`${API_ROOT}/songs`);
  songsStore.songs = await data.json();
}

export async function deleteSong(song: Pick<Song, 'hash'>): Promise<void> {
  const data = await fetch(`${API_ROOT}/songs/delete/${song.hash}`, {
    method: 'DELETE',
  });
  if (data.ok) {
    const index = songsStore.songs.findIndex(s => s.hash === song.hash);
    songsStore.songs.splice(index, 1);
  }
}

export async function startImport(url: string): Promise<void> {
  const data = await fetch(`${API_ROOT}/import/url`, {
    method: 'POST',
    body: `"${url}"`,
  });
  if (data.ok) {
    const json: ImportResponse = await data.json();
    if (json.type === 'Song') {
      songsStore.songs.unshift(json.importedSong);
    } else {
      sendErrorNotification(json.error);
    }
  } else {
    sendErrorNotification(await data.text());
  }
}
