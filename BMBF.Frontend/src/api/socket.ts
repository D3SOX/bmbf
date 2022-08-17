import { API_HOST, sendErrorNotification, sendSuccessNotification } from './base';
import { MessageType, SocketMessage } from '../types/socket';
import { playlistsStore } from './playlists';
import { songsStore } from './songs';
import { modsStore } from './mods';
import { setupStore } from './setup';
import { beatSaberStore } from './beatsaber';
import { SetupStage } from '../types/setup';
import { progressStore } from './progress';

let socket: WebSocket | null = null;

function handleEvent(event: SocketMessage) {
  switch (event.type) {
    case MessageType.SongAdded: {
      const { song } = event;
      songsStore.songs.unshift(song);
      sendSuccessNotification(`Song "${song.songName}" added`);
      break;
    }
    case MessageType.SongRemoved: {
      const songIndex = songsStore.songs.findIndex(s => s.hash === event.hash);
      songsStore.songs.splice(songIndex, 1);
      break;
    }
    case MessageType.PlaylistUpdated: {
      const { playlistInfo, coverUpdated } = event;
      if (coverUpdated) {
        // TODO: force cover image reload
      }
      if (playlistInfo) {
        const playlist = playlistsStore.playlists.find(p => p.id === playlistInfo.id);
        if (playlist) {
          playlist.playlistTitle = playlistInfo.playlistTitle;
          playlist.playlistAuthor = playlistInfo.playlistAuthor;
          playlist.playlistDescription = playlistInfo.playlistDescription;
          playlist.syncSaberFeed = playlistInfo.syncSaberFeed;
        }
      }
      break;
    }
    case MessageType.PlaylistAdded: {
      const { playlistInfo: playlist } = event;
      if (playlist) {
        sendSuccessNotification(`Playlist "${playlist.playlistTitle}" added`);
      }
      break;
    }
    case MessageType.PlaylistRemoved: {
      const playlistIndex = playlistsStore.playlists.findIndex(p => p.id === event.id);
      playlistsStore.playlists.splice(playlistIndex, 1);
      break;
    }
    case MessageType.SetupFinished: {
      setupStore.setupStatus = null;
      break;
    }
    case MessageType.SetupStatusUpdate: {
      setupStore.setupStatus = event.status;
      if (event.status.isInProgress) {
        console.log('now in progress', event);
        switch (event.status.stage) {
          case SetupStage.Downgrading:
            setupStore.loadingStep = 1;
            break;
          case SetupStage.Patching:
            setupStore.loadingStep = 2;
            break;
          case SetupStage.UninstallingOriginal:
            setupStore.loadingStep = 3;
            break;
          case SetupStage.InstallingModded:
            setupStore.loadingStep = 4;
            break;
          case SetupStage.Finalizing:
            setupStore.loadingStep = 5;
            break;
        }
      } else {
        setupStore.loadingStep = null;
      }
      break;
    }
    case MessageType.InstallationUpdated: {
      beatSaberStore.installationInfo = event.installation;
      break;
    }
    case MessageType.ModAdded: {
      const { mod } = event;
      modsStore.mods.unshift(mod);
      sendSuccessNotification(`Mod "${mod.name}" added`);
      break;
    }
    case MessageType.ModRemoved: {
      const modIndex = modsStore.mods.findIndex(m => m.id === event.id);
      modsStore.mods.splice(modIndex, 1);
      break;
    }
    case MessageType.ModStatusChanged: {
      const mod = modsStore.mods.find(m => m.id === event.id);
      if (mod) {
        mod.installed = event.newStatus;
      }
      break;
    }
    case MessageType.ProgressAdded: {
      progressStore.progress.unshift(event.progress);
      break;
    }
    case MessageType.ProgressRemoved: {
      const progressIndex = progressStore.progress.findIndex(p => p.id === event.id);
      progressStore.progress.splice(progressIndex, 1);
      break;
    }
    case MessageType.ProgressUpdated: {
      const progress = progressStore.progress.find(p => p.id === event.id);
      if (progress) {
        progress.completed = event.itemsCompleted;
      }
      break;
    }
  }
}

export function startSocket() {
  if (!socket) {
    const ws = new WebSocket(`ws://${API_HOST}/api/ws`);
    ws.addEventListener('message', socketEvent => {
      try {
        handleEvent(JSON.parse(socketEvent.data));
      } catch (error) {
        console.error('Error while parsing message', error);
        sendErrorNotification('Error while parsing WebSocket message');
      }
    });
    socket = ws;
  }
}

export function stopSocket() {
  if (socket) {
    socket.close();
  }
}
