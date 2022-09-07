import { Mod } from '../types/mod';
import { Badge, Card, Group, Image, Stack, Text, Button, Switch, Modal, Title } from '@mantine/core';
import { IconEye, IconTrash } from '@tabler/icons';
import { installMod, uninstallMod, unloadMod } from '../api/mods';
import { API_ROOT } from '../api/base';
import { useMemo, useState } from 'react';

interface ModCardProps {
  mod: Mod;
}

function SongCard({ mod }: ModCardProps) {
  const [showDetails, setShowDetails] = useState(false);
  const dependencies = useMemo(() => Object.entries(mod.dependencies), [mod.dependencies]);

  return (
    <Card title={mod.id}>
      <Group align="start" noWrap>
        <Image src={`${API_ROOT}/mods/cover/${mod.id}`} alt="Cover" width={150} radius="md" />
        <Stack>
          <Stack spacing={1}>
            <Text size="xl">
              {mod.name} <Badge size="xs">{mod.version}</Badge>
            </Text>
            <Text>Created by {mod.author}</Text>
          </Stack>
          <Group>
            <Switch
              checked={mod.installed}
              onChange={event => {
                event.currentTarget.checked ? installMod(mod) : uninstallMod(mod);
              }}
            />
            <Button leftIcon={<IconEye />} variant="light" onClick={() => setShowDetails(true)}>
              Details
            </Button>
            <Modal
              opened={showDetails}
              onClose={() => setShowDetails(false)}
              title={<Title order={3}>Details for {mod.name}</Title>}
              centered
            >
              <Stack spacing={5}>
                <Stack spacing={1}>
                  <Title order={4}>Description</Title>
                  <Text>{mod.description}</Text>
                </Stack>
                <Stack spacing={1}>
                  <Title order={4}>Dependencies</Title>
                  {dependencies.length ? (
                    <div>
                      {dependencies.map(([dependencyName, dependencyVersion]) => (
                        <Text key={dependencyName}>
                          {dependencyName} {dependencyVersion}
                        </Text>
                      ))}
                    </div>
                  ) : (
                    <Text>None</Text>
                  )}
                </Stack>
              </Stack>
            </Modal>
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
