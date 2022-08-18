import { Stack, Stepper, Title, Button, Text, Card } from '@mantine/core';
import { useSnapshot } from 'valtio';
import {
  begin,
  downgrade,
  finalize,
  needsDowngrade,
  patch,
  quit,
  setupStore,
  triggerInstall,
  triggerUninstall,
} from '../api/setup';
import { useMemo } from 'react';
import {
  IconArrowDown,
  IconArrowRight,
  IconFlag,
  IconFlag3,
  IconLogout,
  IconPackage,
  IconTrash,
  IconWand,
} from '@tabler/icons';
import { SetupStage } from '../types/setup';
import { beatSaberStore } from '../api/beatsaber';

function Setup() {
  const { setupStatus, loadingStep } = useSnapshot(setupStore);
  const { installationInfo } = useSnapshot(beatSaberStore);

  const active = useMemo(() => {
    if (setupStatus === null) return 0;
    switch (setupStatus.stage) {
      case SetupStage.Downgrading:
        return needsDowngrade() ? 1 : 2;
      case SetupStage.Patching:
        return 2;
      case SetupStage.UninstallingOriginal:
        return 3;
      case SetupStage.InstallingModded:
        return 4;
      case SetupStage.Finalizing:
        return 5;
      default:
        return 0;
    }
  }, [setupStatus]);

  return (
    <Stack align="center">
      <img src="/logo.png" alt="Logo" />
      <Title>Setup</Title>
      {setupStatus && (
        <Button variant="outline" color="orange" leftIcon={<IconLogout />} onClick={quit}>
          Exit (Restarts the setup)
        </Button>
      )}

      {!installationInfo && active != 4 ? (
        <Text>Beat Saber is not installed</Text>
      ) : (
        <Stepper active={active} breakpoint="sm">
          <Stepper.Step
            label="Start"
            description="Start the setup"
            icon={<IconFlag />}
            loading={loadingStep === 0}
          >
            <Card>
              <Stack align="start">
                <Text>Start the initial setup</Text>
                <Button onClick={begin} disabled={loadingStep !== null} leftIcon={<IconArrowRight />}>
                  Next
                </Button>
              </Stack>
            </Card>
          </Stepper.Step>
          <Stepper.Step
            label="Downgrade"
            description="Downgrade the game"
            icon={<IconArrowDown />}
            loading={loadingStep === 1}
            sx={{ display: needsDowngrade() ? 'block' : 'none' }}
          >
            <Card>
              <Stack align="start">
                <Text>Your game needs a downgrade</Text>
                <Button
                  onClick={() => downgrade('TODO')}
                  disabled={loadingStep !== null}
                  leftIcon={<IconArrowRight />}
                >
                  Downgrade
                </Button>
              </Stack>
            </Card>
          </Stepper.Step>
          <Stepper.Step
            label="Patch"
            description="Patch the game"
            icon={<IconWand />}
            loading={loadingStep === 2}
          >
            <Card>
              <Stack align="start">
                <Text>Now patch the game</Text>
                <Button onClick={patch} disabled={loadingStep !== null} leftIcon={<IconArrowRight />}>
                  Next
                </Button>
              </Stack>
            </Card>
          </Stepper.Step>
          <Stepper.Step
            label="Uninstall"
            description="Uninstall the original game"
            icon={<IconTrash />}
            loading={loadingStep === 3}
          >
            <Card>
              <Stack align="start">
                <Text>Now uninstall the original game</Text>
                <Button
                  onClick={triggerUninstall}
                  disabled={loadingStep !== null}
                  leftIcon={<IconArrowRight />}
                >
                  Next
                </Button>
              </Stack>
            </Card>
          </Stepper.Step>
          <Stepper.Step
            label="Install"
            description="Install the modded game"
            icon={<IconPackage />}
            loading={loadingStep === 4}
          >
            <Card>
              <Stack align="start">
                <Text>Now install the modded game</Text>
                <Button
                  onClick={triggerInstall}
                  disabled={loadingStep !== null}
                  leftIcon={<IconArrowRight />}
                >
                  Next
                </Button>
              </Stack>
            </Card>
          </Stepper.Step>
          <Stepper.Step
            label="Finalize"
            description="Finalize the setup"
            icon={<IconFlag3 />}
            loading={loadingStep === 5}
          >
            <Card>
              <Stack align="start">
                <Text>Now finalize the setup</Text>
                <Button onClick={finalize} disabled={loadingStep !== null} leftIcon={<IconArrowRight />}>
                  Next
                </Button>
              </Stack>
            </Card>
          </Stepper.Step>
        </Stepper>
      )}
    </Stack>
  );
}

export default Setup;
