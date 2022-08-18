import { Grid, Stack, Title, Text } from '@mantine/core';
import { useEffect } from 'react';
import { fetchSongs, songsStore } from '../api/songs';
import SongCard from '../components/SongCard';
import { useSnapshot } from 'valtio';

function Songs() {
  const { songs } = useSnapshot(songsStore);

  useEffect(() => {
    fetchSongs();
  }, []);

  return (
    <Stack align="center">
      <img src="/logo.png" alt="Logo" />
      <Title>Songs</Title>
      {songs.length ? (
        <Grid gutter="md" grow>
          {songs.map(song => (
            <Grid.Col key={song.hash} md={6} lg={4}>
              <SongCard song={song} />
            </Grid.Col>
          ))}
        </Grid>
      ) : (
        <Text>No songs found</Text>
      )}
    </Stack>
  );
}

export default Songs;
