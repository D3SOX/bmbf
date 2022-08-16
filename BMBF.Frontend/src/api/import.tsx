import { API_ROOT, sendErrorNotification, sendSuccessNotification } from './base';
import { ImportResponse, ImportType } from '../types/import';
import { songsStore } from './songs';
import { fetchPlaylists, playlistsStore } from './playlists';
import { modsStore } from './mods';

export async function startImport(url: string): Promise<void> {
  const data = await fetch(`${API_ROOT}/import/url`, {
    method: 'POST',
    body: `"${url}"`,
  });
  if (data.ok) {
    const json: ImportResponse = await data.json();
    if (json.type === ImportType.Song) {
      // this can be removed when the websocket is implemented
      songsStore.songs.unshift(json.importedSong);
      sendSuccessNotification(`Song "${json.importedSong.songName}" imported`);
    } else if (json.type === ImportType.Playlist) {
      // refresh for now as it does not return the actual playlist here, can be removed when the websocket is implemented
      await fetchPlaylists();
      const playlist = playlistsStore.playlists.find(p => p.id === json.importedPlaylistId);
      if (playlist) {
        sendSuccessNotification(`Playlist "${playlist.playlistTitle}" imported`);
      }
    } else if (json.type === ImportType.Mod) {
      // this can be removed when the websocket is implemented
      modsStore.mods.unshift(json.importedMod);
      sendSuccessNotification(`Mod "${json.importedMod.name}" imported`);
    } else {
      sendErrorNotification(json.error);
    }
  } else {
    sendErrorNotification(await data.text());
  }
}
