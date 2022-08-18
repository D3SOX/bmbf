export interface Playlist {
  id: string;
  playlistTitle: string;
  playlistAuthor: string;
  playlistDescription: string;
  syncSaberFeed: string | null;
}

export interface PlaylistSong {
  hash: string;
  key: string;
  name: string;
  uploader: string;
}
