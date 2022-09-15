import { Playlist, PlaylistSong } from '../types/playlist';
import { backendRequest } from './base';
import { proxy } from 'valtio';

export const playlistsStore = proxy<{ playlists: Playlist[] }>({ playlists: [] });

export async function fetchPlaylists(): Promise<void> {
  const data = await backendRequest('playlists');
  if (data.ok) {
    playlistsStore.playlists = await data.json();
  }
}

export async function getPlaylistSongs(
  playlist: Pick<Playlist, 'id'>
): Promise<PlaylistSong[] | undefined> {
  const data = await backendRequest(`playlists/songs/${playlist.id}`);
  if (data.ok) {
    return data.json();
  }
}

export async function deletePlaylist(playlist: Pick<Playlist, 'id'>): Promise<void> {
  await backendRequest(`playlists/delete/${playlist.id}`, {
    method: 'DELETE',
  });
}
