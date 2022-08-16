import { Song } from './song';

export interface FailedImportResponse {
  type: 'Failed';
  error: string;
}

export interface SongImportResponse {
  type: 'Song';
  importedSong: Song;
}

export type ImportResponse = FailedImportResponse | SongImportResponse;
