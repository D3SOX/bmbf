import { Song } from './song';
import { Mod } from './mod';

export const enum ImportType {
  Song = 'Song',
  Mod = 'Mod',
  FileCopy = 'FileCopy',
  Config = 'Config',
  Playlist = 'Playlist',
  Failed = 'Failed',
}

export interface BaseImportResponse {
  type: ImportType;
}

export interface SongImportResponse extends BaseImportResponse {
  type: ImportType.Song;
  importedSong: Song;
}

export interface ModImportResponse extends BaseImportResponse {
  type: ImportType.Mod;
  importedMod: Mod;
}

export interface FileCopyImportResponse extends BaseImportResponse {
  type: ImportType.FileCopy;
  fileCopy: null;
}

export interface ConfigImportResponse extends BaseImportResponse {
  type: ImportType.Config;
  configModId: string;
}

export interface PlaylistImportResponse extends BaseImportResponse {
  type: ImportType.Playlist;
  importedPlaylistId: string;
}

export interface FailedImportResponse extends BaseImportResponse {
  type: ImportType.Failed;
  error: string;
}

export type ImportResponse =
  | SongImportResponse
  | ModImportResponse
  | FileCopyImportResponse
  | ConfigImportResponse
  | PlaylistImportResponse
  | FailedImportResponse;
