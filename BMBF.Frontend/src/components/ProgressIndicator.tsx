import { Notification, Portal, Progress, Stack, Text } from '@mantine/core';
import React from 'react';
import { useSnapshot } from 'valtio';
import { progressStore } from '../api/progress';

interface ProgressBarProps {
  completed: number;
  total: number;
}
function ProgressBar({ completed, total }: ProgressBarProps) {
  const percent = Math.round((completed / total) * 100);

  return <Progress value={percent} label={`${percent}%`} size="xl" />;
}

function ProgressIndicator() {
  const { progress } = useSnapshot(progressStore);

  return (
    <Portal target="header">
      {progress.length > 0 && (
        <Stack>
          {progress.map(({ id, name, completed, total, representAsPercentage }) => (
            <Notification
              key={id}
              title={representAsPercentage ? `${name} (${completed}/${total})` : name}
              disallowClose
            >
              {representAsPercentage ? (
                <ProgressBar completed={completed} total={total} />
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
