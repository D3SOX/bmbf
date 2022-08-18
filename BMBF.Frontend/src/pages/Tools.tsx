import { Button, Group, Stack, TextInput, Title, Text } from '@mantine/core';
import { useInputState } from '@mantine/hooks';
import { startImport } from '../api/import';
import { IconArrowRight, IconWorldDownload } from '@tabler/icons';
import { useState } from 'react';
import { useSnapshot } from 'valtio';
import { beatSaberStore } from '../api/beatsaber';

function Tools() {
  const [busy, setBusy] = useState(false);
  const [url, setUrl] = useInputState('');

  const { installationInfo } = useSnapshot(beatSaberStore);

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

  async function handleImport() {
    setBusy(true);
    await startImport(url);
    setBusy(false);
    setUrl('');
  }

  return (
    <Stack align="center">
      <img src="/logo.png" alt="Logo" />
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
        <Button onClick={handleImport} leftIcon={<IconArrowRight />} disabled={busy} loading={busy}>
          Start import
        </Button>
      </Group>
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
