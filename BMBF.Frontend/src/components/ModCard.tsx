import { Mod } from '../types/mod';
import {
  ActionIcon,
  Badge,
  Card,
  Group,
  Image,
  Stack,
  Text,
  Button,
  Switch,
  Modal,
  Title,
  List,
  Tooltip,
} from '@mantine/core';
import { IconArrowBackUp, IconEye, IconPlugConnected, IconToggleLeft, IconTrash } from '@tabler/icons';
import { installMod, modsStore, uninstallMod, unloadMod } from '../api/mods';
import { API_ROOT } from '../api/base';
import { useMemo, useState } from 'react';
import { useSnapshot } from 'valtio';

interface ModCardProps {
  mod: Mod;
}

function SongCard({ mod }: ModCardProps) {
  const [showRemoveModal, setShowRemoveModal] = useState(false);
  const [showDisableModal, setShowDisableModal] = useState(false);
  const [showDetailsModal, setShowDetailsModal] = useState(false);
  const dependencies = useMemo(() => Object.entries(mod.dependencies), [mod.dependencies]);

  const { mods } = useSnapshot(modsStore);
  const dependants = useMemo(() => {
    // installed & enabled mods that depend on this mod
    return mods.filter(
      otherMod =>
        otherMod.id !== mod.id && otherMod.dependencies[mod.id] !== undefined && otherMod.installed
    );
  }, [mod, mods]);

  return (
    <Card title={mod.id}>
      <Modal
        opened={showDetailsModal}
        onClose={() => setShowDetailsModal(false)}
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

      <Modal
        opened={showRemoveModal}
        onClose={() => setShowRemoveModal(false)}
        title={<Title order={3}>Remove {mod.name}?</Title>}
        withCloseButton={false}
        centered
      >
        <Stack>
          {dependants.length ? (
            <Stack spacing={1}>
              <Title order={4}>Warning</Title>
              <Text>This mod is required by the following enabled mods:</Text>
              <List>
                {dependants.map(m => (
                  <List.Item key={m.id}>{m.name}</List.Item>
                ))}
              </List>
              <Text>Removing this mod will also disable the above mods.</Text>
            </Stack>
          ) : (
            <Text>There are no mods installed that depend on this mod.</Text>
          )}
          <Group grow>
            <Button leftIcon={<IconArrowBackUp />} onClick={() => setShowRemoveModal(false)}>
              No, don't remove
            </Button>
            <Button leftIcon={<IconTrash />} color="red" onClick={() => unloadMod(mod)}>
              Yes, remove
            </Button>
          </Group>
        </Stack>
      </Modal>

      <Modal
        opened={showDisableModal}
        onClose={() => setShowDisableModal(false)}
        title={<Title order={3}>Disable {mod.name}?</Title>}
        withCloseButton={false}
        centered
      >
        <Stack>
          {dependants.length ? (
            <Stack spacing={1}>
              <Title order={4}>Warning</Title>
              <Text>This mod is required by the following enabled mods:</Text>
              <List>
                {dependants.map(m => (
                  <List.Item key={m.id}>{m.name}</List.Item>
                ))}
              </List>
              <Text>Disabling this mod will also disable the above mods.</Text>
            </Stack>
          ) : (
            <Text>There are no mods installed that depend on this mod.</Text>
          )}
          <Group grow>
            <Button leftIcon={<IconArrowBackUp />} onClick={() => setShowDisableModal(false)}>
              No, don't disable
            </Button>
            <Button
              leftIcon={<IconToggleLeft />}
              color="red"
              onClick={() => {
                setShowDisableModal(false);
                uninstallMod(mod);
              }}
            >
              Yes, disable
            </Button>
          </Group>
        </Stack>
      </Modal>

      <Group align="start" noWrap>
        <Image src={`${API_ROOT}/mods/cover/${mod.id}`}
          alt="Cover"
          width={200}
          height={113}
          radius="md"
          withPlaceholder
          placeholder={<IconPlugConnected size={36}/>} 
        />
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
                if (event.currentTarget.checked) {
                  installMod(mod);
                } else if (dependants.length) {
                  setShowDisableModal(true);
                } else {
                  uninstallMod(mod);
                }
              }}
            />
            <Tooltip label="Show details">
              <ActionIcon variant="transparent" color="indigo" onClick={() => setShowDetailsModal(true)}>
                <IconEye />
              </ActionIcon>
            </Tooltip>
            <Button
              leftIcon={<IconTrash />}
              color="red"
              variant="light"
              onClick={() => setShowRemoveModal(true)}
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
