import { Button, Group, Stack, TextInput, Title } from '@mantine/core';
import { useInputState } from '@mantine/hooks';
import { startImport } from '../api/import';
import { IconArrowRight, IconWorldDownload } from '@tabler/icons';
import { useState } from 'react';

function Tools() {
  const [busy, setBusy] = useState(false);
  const [url, setUrl] = useInputState('');

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
    </Stack>
  );
}

export default Tools;
