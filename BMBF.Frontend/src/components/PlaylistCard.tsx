import { Playlist } from '../types/playlist';
import { Card, Group, Image, Stack, Text, Button } from '@mantine/core';
import { IconPlaylist, IconTrash } from '@tabler/icons';
import { deletePlaylist } from '../api/playlists';
import { API_ROOT } from '../api/base';

interface PlaylistCardProps {
  playlist: Playlist;
}

function PlaylistCard({ playlist }: PlaylistCardProps) {
  return (
    <Card title={playlist.id} shadow="md">
      <Group align="start" noWrap>
        <Image
          src={`${API_ROOT}/playlists/cover/${playlist.id}`}
          alt="Cover"
          width={150}
          height={150}
          radius="md"
          withPlaceholder
          placeholder={<IconPlaylist size={36} />}
        />
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
