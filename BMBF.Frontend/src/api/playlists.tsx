import { API_ROOT } from './base';
import { proxy } from 'valtio';
import { Playlist, PlaylistSong } from '../types/playlist';

export const playlistsStore = proxy<{ playlists: Playlist[] }>({ playlists: [] });

export async function fetchPlaylists(): Promise<void> {
  const data = await fetch(`${API_ROOT}/playlists`);
  playlistsStore.playlists = await data.json();
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
