import { Playlist } from '../types/playlist';
import { Card, Group, Image, Stack, Text, Button } from '@mantine/core';
import { IconTrash } from '@tabler/icons';
import { deletePlaylist } from '../api/playlists';
import { API_ROOT } from '../api/base';

interface PlaylistCardProps {
  playlist: Playlist;
}

function PlaylistCard({ playlist }: PlaylistCardProps) {
  return (
    <Card title={playlist.id}>
      <Group align="start" noWrap>
        <Image src={`${API_ROOT}/playlists/cover/${playlist.id}`} alt="Cover" width={150} radius="md" />
        <Stack>
          <Stack spacing={1}>
            <Text size="xl">{playlist.playlistTitle}</Text>
            <Text>Created by {playlist.playlistAuthor}</Text>
          </Stack>
          <Group>
            <Button
              leftIcon={<IconTrash />}
              color="red"
              variant="light"
              onClick={() => deletePlaylist(playlist)}
            >
              Remove
            </Button>
          </Group>
        </Stack>
      </Group>
    </Card>
  );
}

export default PlaylistCard;
