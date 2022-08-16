import { Grid, Stack, Title, Text } from '@mantine/core';
import { useEffect } from 'react';
import { fetchPlaylists, playlistsStore } from '../api/playlists';
import PlaylistCard from '../components/PlaylistCard';
import { useSnapshot } from 'valtio';

function Playlists() {
  const { playlists } = useSnapshot(playlistsStore);

  useEffect(() => {
    fetchPlaylists();
  }, []);

  return (
    <Stack align="center">
      <img src="/logo.png" alt="Logo" />
      <Title>Playlists</Title>
      {playlists.length ? (
        <Grid gutter="md" grow>
          {playlists.map(playlist => (
            <Grid.Col key={playlist.id} md={6} lg={4}>
              <PlaylistCard playlist={playlist} />
            </Grid.Col>
          ))}
        </Grid>
      ) : (
        <Text>No playlists found</Text>
      )}
    </Stack>
  );
}

export default Playlists;
