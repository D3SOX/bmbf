import { Song } from './song';

export interface FailedImportResponse {
  type: 'Failed';
  error: string;
}

export interface SongImportResponse {
  type: 'Song';
  importedSong: Song;
}

export interface PlaylistImportResponse {
  type: 'Playlist';
  importedPlaylistId: string;
}

export type ImportResponse = FailedImportResponse | SongImportResponse | PlaylistImportResponse;
