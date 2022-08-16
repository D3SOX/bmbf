import { Mod } from '../types/mod';
import { Card, Group, Image, Stack, Text, Button } from '@mantine/core';
import { IconPackage, IconTrash } from '@tabler/icons';
import { installMod, uninstallMod } from '../api/mods';
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
            {mod.installed ? (
              <Button
                leftIcon={<IconTrash />}
                color="red"
                variant="light"
                onClick={() => uninstallMod(mod)}
              >
                Uninstall
              </Button>
            ) : (
              <Button
                leftIcon={<IconPackage />}
                color="green"
                variant="light"
                onClick={() => installMod(mod)}
              >
                Install
              </Button>
            )}
          </Group>
        </Stack>
      </Group>
    </Card>
  );
}

export default SongCard;
