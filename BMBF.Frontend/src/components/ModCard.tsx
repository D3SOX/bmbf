import { Mod } from '../types/mod';
import { Card, Group, Image, Stack, Text, Button, Switch } from '@mantine/core';
import { IconTrash } from '@tabler/icons';
import { installMod, uninstallMod, unloadMod } from '../api/mods';
import { API_ROOT } from '../api/base';

interface ModCardProps {
  mod: Mod;
}

function SongCard({ mod }: ModCardProps) {
  return (
    <Card title={mod.id}>
      <Group align="start" noWrap>
        <Image src={`${API_ROOT}/mods/cover/${mod.id}`} alt="Cover" width={150} radius="md" />
        <Stack>
          <Stack spacing={1}>
            <Text size="xl">{mod.name}</Text>
            <Text>Created by {mod.author}</Text>
          </Stack>
          <Group>
            <Switch
              checked={mod.installed}
              onChange={event => {
                event.currentTarget.checked ? installMod(mod) : uninstallMod(mod);
              }}
            />
            <Button leftIcon={<IconTrash />} color="red" variant="light" onClick={() => unloadMod(mod)}>
              Remove
            </Button>
          </Group>
        </Stack>
      </Group>
    </Card>
  );
}

export default SongCard;
