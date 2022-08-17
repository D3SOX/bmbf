import { Song } from './song';
import { Playlist } from './playlist';
import { SetupStatus } from './setup';
import { InstallationInfo } from './beatsaber';
import { Mod } from './mod';
import { Progress } from './progress';

export const enum MessageType {
  SongAdded = 'SongAdded',
  SongRemoved = 'SongRemoved',

  PlaylistUpdated = 'PlaylistUpdated',
  PlaylistAdded = 'PlaylistAdded',
  PlaylistRemoved = 'PlaylistRemoved',

  SetupFinished = 'SetupFinished',
  SetupStatusUpdate = 'SetupStatusUpdate',

  InstallationUpdated = 'InstallationUpdated',

  ModAdded = 'ModAdded',
  ModRemoved = 'ModRemoved',
  ModStatusChanged = 'ModStatusChanged',

  ProgressAdded = 'ProgressAdded',
  ProgressRemoved = 'ProgressRemoved',
  ProgressUpdated = 'ProgressUpdated',
}

export interface BaseSocketMessage {
  type: MessageType;
}

export interface SongAdded extends BaseSocketMessage {
  type: MessageType.SongAdded;
  song: Song;
}

export interface SongRemoved extends BaseSocketMessage {
  type: MessageType.SongRemoved;
  hash: string;
}

export interface PlaylistUpdated extends BaseSocketMessage {
  type: MessageType.PlaylistUpdated;
  songs: Song[] | null;
  playlistInfo: Playlist | null;
  coverUpdated: boolean;
}

export interface PlaylistAdded extends BaseSocketMessage {
  type: MessageType.PlaylistAdded;
  playlistInfo: Playlist;
}

export interface PlaylistRemoved extends BaseSocketMessage {
  type: MessageType.PlaylistRemoved;
  id: string;
}

export interface SetupFinished extends BaseSocketMessage {
  type: MessageType.SetupFinished;
}

export interface SetupStatusUpdate extends BaseSocketMessage {
  type: MessageType.SetupStatusUpdate;
  status: SetupStatus;
}

export interface InstallationUpdated extends BaseSocketMessage {
  type: MessageType.InstallationUpdated;
  installation: InstallationInfo | null; // Null if Beat Saber is no longer installed
}

export interface ModAdded extends BaseSocketMessage {
  type: MessageType.ModAdded;
  mod: Mod;
}

export interface ModRemoved extends BaseSocketMessage {
  type: MessageType.ModRemoved;
  id: string;
}

export interface ModStatusChanged extends BaseSocketMessage {
  type: MessageType.ModStatusChanged;
  id: string;
  newStatus: boolean;
}

export interface ProgressAdded extends BaseSocketMessage {
  type: MessageType.ProgressAdded;
  progress: Progress;
}

export interface ProgressRemoved extends BaseSocketMessage {
  type: MessageType.ProgressRemoved;
  id: number;
}

export interface ProgressUpdated extends BaseSocketMessage {
  type: MessageType.ProgressUpdated;
  id: number;
  itemsCompleted: number;
}

export type SocketMessage =
  | SongAdded
  | SongRemoved
  | PlaylistUpdated
  | PlaylistAdded
  | PlaylistRemoved
  | SetupFinished
  | SetupStatusUpdate
  | InstallationUpdated
  | ModAdded
  | ModRemoved
  | ModStatusChanged
  | ProgressAdded
  | ProgressRemoved
  | ProgressUpdated;
