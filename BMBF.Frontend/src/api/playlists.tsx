import { Playlist, PlaylistSong } from '../types/playlist';
import { API_ROOT, sendErrorNotification } from './base';
import { proxy } from 'valtio';

export const playlistsStore = proxy<{ playlists: Playlist[] }>({ playlists: [] });

export async function fetchPlaylists(): Promise<void> {
  const data = await fetch(`${API_ROOT}/playlists`);
  if (data.ok) {
    playlistsStore.playlists = await data.json();
  } else {
    sendErrorNotification(await data.text());
  }
}

export async function getPlaylistSongs(
  playlist: Pick<Playlist, 'id'>
): Promise<PlaylistSong[] | undefined> {
  const data = await fetch(`${API_ROOT}/playlists/songs/${playlist.id}`);
  if (data.ok) {
    return data.json();
  }
}

export async function deletePlaylist(playlist: Pick<Playlist, 'id'>): Promise<void> {
  const data = await fetch(`${API_ROOT}/playlists/delete/${playlist.id}`, {
    method: 'DELETE',
  });
  // this can be removed when the websocket is implemented
  if (data.ok) {
    const index = playlistsStore.playlists.findIndex(p => p.id === playlist.id);
    playlistsStore.playlists.splice(index, 1);
  }
}
