import { Stack, Title, Text } from '@mantine/core';
import { useEffect } from 'react';
import { fetchSongs, songsStore } from '../api/songs';
import SongCard from '../components/SongCard';
import { useSnapshot } from 'valtio';
import { Masonry } from 'masonic';

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
        <Masonry
          items={songsStore.songs}
          render={SongCard}
          columnGutter={15}
          columnWidth={400}
          itemHeightEstimate={150}
        />
      ) : (
        <Text>No songs found</Text>
      )}
    </Stack>
  );
}

export default Songs;
