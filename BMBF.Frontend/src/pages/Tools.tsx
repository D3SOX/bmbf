import { Button, Group, Stack, TextInput, Title, Text, Card, useMantineTheme } from '@mantine/core';
import { useInputState } from '@mantine/hooks';
import { importViaFile, importViaUrl } from '../api/import';
import { IconArrowRight, IconFileUpload, IconUpload, IconWorldDownload, IconX } from '@tabler/icons';
import { useState } from 'react';
import { useSnapshot } from 'valtio';
import { beatSaberStore } from '../api/beatsaber';
import { Dropzone } from '@mantine/dropzone';
import { infoStore } from '../api/info';
import ToolButtons from '../components/ToolButtons';

function Tools() {
  const theme = useMantineTheme();
  const [busy, setBusy] = useState(false);
  const [url, setUrl] = useInputState('');

  const { installationInfo } = useSnapshot(beatSaberStore);
  const { hostInfo } = useSnapshot(infoStore);

  function ModInfo() {
    if (installationInfo?.modTag) {
      return (
        <>
          <Text>Patcher name: {installationInfo.modTag.patcherName}</Text>
          <Text>Patcher version: {installationInfo.modTag.patcherVersion || 'Not available'}</Text>
          <Text>Modloader name: {installationInfo.modTag.modloaderName || 'Not available'}</Text>
          <Text>Modloader version: {installationInfo.modTag.modloaderVersion || 'Not available'}</Text>
        </>
      );
    }
    return <Text>Game not modded</Text>;
  }

  async function handleUrlImport() {
    setBusy(true);
    await importViaUrl(url);
    setBusy(false);
    setUrl('');
  }

  async function handleFileImport(files: File[]) {
    setBusy(true);
    await importViaFile(files[0]);
    setBusy(false);
  }

  return (
    <Stack align="center">
      <Title>Tools</Title>
      <Title order={2}>Import via URL</Title>
      <Group>
        <TextInput
          value={url}
          onChange={setUrl}
          placeholder="URL to import"
          icon={<IconWorldDownload />}
          disabled={busy}
        />
        <Button onClick={handleUrlImport} leftIcon={<IconArrowRight />} disabled={busy} loading={busy}>
          Start import
        </Button>
      </Group>
      <Title order={2}>Import via File</Title>
      <Card radius="md" shadow="md">
        <Dropzone onDrop={handleFileImport} multiple={false} loading={busy}>
          <Group position="center" spacing="xl" style={{ minHeight: 220, pointerEvents: 'none' }}>
            <Dropzone.Accept>
              <IconUpload
                size={50}
                stroke={1.5}
                color={theme.colors[theme.primaryColor][theme.colorScheme === 'dark' ? 4 : 6]}
              />
            </Dropzone.Accept>
            <Dropzone.Reject>
              <IconX
                size={50}
                stroke={1.5}
                color={theme.colors.red[theme.colorScheme === 'dark' ? 4 : 6]}
              />
            </Dropzone.Reject>
            <Dropzone.Idle>
              <IconFileUpload size={50} stroke={1.5} />
            </Dropzone.Idle>

            <div>
              <Text size="xl" inline>
                Drag Mods / Playlists / Songs here or click to select files
              </Text>
              <Text size="sm" color="dimmed" inline mt={7}>
                Attach any supported file as you like, multiple files are not yet supported
              </Text>
            </div>
          </Group>
        </Dropzone>
      </Card>
      <Title order={2}>Host Info</Title>
      {hostInfo ? (
        <Stack spacing={1}>
          <Text>Version: {hostInfo.version}</Text>
          <Text>Host Local IP: {hostInfo.hostLocalIp}</Text>
          <Text>Accessible via browser at http://{hostInfo.hostLocalIp}:50005</Text>
          <Text>Connecting IP: {hostInfo.connectingIp}</Text>
        </Stack>
      ) : (
        <Text>Host info not loaded yet</Text>
      )}
      <Title order={2}>Backend</Title>
      <ToolButtons />
      <Title order={2}>Debug info</Title>
      {installationInfo ? (
        <Stack spacing={1}>
          <Text>APK Version: {installationInfo.version}</Text>
          <Text>APK Version code: {installationInfo.versionCode}</Text>
          <Title order={3}>Mod Info</Title>
          <ModInfo />
        </Stack>
      ) : (
        <Text>Game not installed</Text>
      )}
    </Stack>
  );
}

export default Tools;
