import { API_ROOT, sendErrorNotification, sendSuccessNotification } from './base';
import { ImportResponse } from '../types/import';
import { songsStore } from './songs';
import { fetchPlaylists, playlistsStore } from './playlists';

export async function startImport(url: string): Promise<void> {
  const data = await fetch(`${API_ROOT}/import/url`, {
    method: 'POST',
    body: `"${url}"`,
  });
  if (data.ok) {
    const json: ImportResponse = await data.json();
    if (json.type === 'Song') {
      songsStore.songs.unshift(json.importedSong);
      sendSuccessNotification(`Song "${json.importedSong.songName}" imported`);
    } else if (json.type === 'Playlist') {
      await fetchPlaylists();
      const playlist = playlistsStore.playlists.find(p => p.id === json.importedPlaylistId);
      if (playlist) {
        sendSuccessNotification(`Playlist "${playlist.playlistTitle}" imported`);
      }
    } else {
      sendErrorNotification(json.error);
    }
  } else {
    sendErrorNotification(await data.text());
  }
}
