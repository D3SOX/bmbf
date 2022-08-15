import { Stack, Title, TextInput, Group, Button } from '@mantine/core';
import { useEffect } from 'react';
import { fetchSongs, songsStore, startImport } from '../api/songs';
import SongCard from '../components/SongCard';
import { useInputState } from '@mantine/hooks';
import { IconArrowRight, IconWorldDownload } from '@tabler/icons';
import { useSnapshot } from 'valtio';

function Songs() {
  const [url, setUrl] = useInputState('');
  const { songs } = useSnapshot(songsStore);

  useEffect(() => {
    fetchSongs();
  }, []);

  return (
    <Stack align="center">
      <img src="/logo.png" alt="Logo" />
      <Title>Songs</Title>
      <Group>
        <TextInput
          value={url}
          onChange={setUrl}
          placeholder="URL to import"
          icon={<IconWorldDownload />}
        />
        <Button onClick={() => startImport(url)} leftIcon={<IconArrowRight />}>
          Start import
        </Button>
      </Group>
      <Stack>
        {songs.map(song => (
          <SongCard key={song.hash} song={song} />
        ))}
      </Stack>
    </Stack>
  );
}

export default Songs;
