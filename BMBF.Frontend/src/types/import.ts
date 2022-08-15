import { Song } from './song';

export interface ImportResponse {
  type: 'Song' | 'Failed'; // TODO: this type can be improved, when it's 'Failed' error is set, otherwise importedSong
  error?: string;
  importedSong?: Song;
}
