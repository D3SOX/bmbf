import { Button, Stack, Switch } from '@mantine/core';
import { IconDoorExit, IconFileText, IconReload } from '@tabler/icons';
import { getRunInBackground, logs, quit, restart, setRunInBackground } from '../api/base';
import { useEffect, useState } from 'react';

function ToolButtons() {
  const [background, setBackground] = useState(false);

  useEffect(() => {
    (async () => {
      setBackground(await getRunInBackground());
    })();
  }, []);

  return (
    <Stack>
      <Button leftIcon={<IconDoorExit />} onClick={() => quit()}>
        Quit BMBF
      </Button>
      <Button leftIcon={<IconReload />} onClick={() => restart()}>
        Restart BMBF Service
      </Button>
      <Button leftIcon={<IconFileText />} onClick={() => logs()}>
        Download Logs
      </Button>
      <Switch
        checked={background}
        label="Run in background"
        onChange={event => {
          const { checked } = event.currentTarget;
          setRunInBackground(checked).then(() => setBackground(checked));
        }}
      />
    </Stack>
  );
}

export default ToolButtons;
