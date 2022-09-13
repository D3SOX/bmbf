import { Notification, Portal, Progress, Stack, Text } from '@mantine/core';
import React from 'react';
import { useSnapshot } from 'valtio';
import { progressStore } from '../api/progress';

function ProgressIndicator() {
  const { progress } = useSnapshot(progressStore);

  return (
    <Portal target="header">
      {progress.length > 0 && (
        <Stack>
          {progress.map(({ id, name, completed, total, representAsPercentage }) => (
            <Notification key={id} title={name} disallowClose>
              {representAsPercentage ? (
                <Progress value={Math.round((completed / total) * 100)} />
              ) : (
                <Text>
                  Progress: {completed}/{total}
                </Text>
              )}
            </Notification>
          ))}
        </Stack>
      )}
    </Portal>
  );
}

export default ProgressIndicator;
