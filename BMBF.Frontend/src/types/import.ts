import { Song } from './song';
import { Mod } from './mod';

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

export interface ModImportResponse {
  type: 'Mod';
  importedMod: Mod;
}

export type ImportResponse =
  | FailedImportResponse
  | SongImportResponse
  | PlaylistImportResponse
  | ModImportResponse;
