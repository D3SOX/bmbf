import { Song } from '../types/song';
import { Card, Group, Image, Stack, Text, Button } from '@mantine/core';
import { IconTrash } from '@tabler/icons';
import { deleteSong } from '../api/songs';
import { API_ROOT } from '../api/base';

interface SongCardProps {
  song: Song;
}

function SongCard({ song }: SongCardProps) {
  return (
    <Card title={song.hash}>
      <Group align="start" noWrap>
        <Image src={`${API_ROOT}/songs/cover/${song.hash}`} alt="Cover" width={150} radius="md" />
        <Stack>
          <Stack spacing={1}>
            <Text size="xl">
              {song.songName} - {song.songAuthorName}
            </Text>
            <Text>Mapped by {song.levelAuthorName}</Text>
          </Stack>
          <Group>
            <Button
              leftIcon={<IconTrash />}
              color="red"
              variant="light"
              onClick={() => deleteSong(song)}
            >
              Remove
            </Button>
          </Group>
        </Stack>
      </Group>
    </Card>
  );
}

export default SongCard;
