import {
  AppShell,
  MantineProvider,
  Modal,
  Title,
  Text,
  Loader,
  ColorSchemeProvider,
  ColorScheme,
  Stack,
  useMantineTheme,
} from '@mantine/core';
import AppHeader from './components/shell/AppHeader';
import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import Home from './pages/Home';
import Mods from './pages/Mods';
import Playlists from './pages/Playlists';
import Songs from './pages/Songs';
import SyncSaber from './pages/SyncSaber';
import Tools from './pages/Tools';
import { NotificationsProvider } from '@mantine/notifications';
import React, { useEffect, useState } from 'react';
import { beatSaberStore, fetchInstallationInfo } from './api/beatsaber';
import Setup from './pages/Setup';
import { fetchModdableVersions, fetchSetupStatus, setupStore } from './api/setup';
import { startSocket, stopSocket, useIsSocketClosed, useSocketEvent } from './api/socket';
import { useSnapshot } from 'valtio';
import { fetchHostInfo } from './api/info';
import { useHotkeys, useLocalStorage } from '@mantine/hooks';
import ProgressIndicator from './components/ProgressIndicator';
import { IconPlugConnectedX } from '@tabler/icons';

export default function App() {
  const [subsequentConnect, setSubsequentConnect] = useState(false);

  useEffect(() => {
    // connect to websocket
    startSocket();

    // disconnect on unmount
    return () => {
      stopSocket();
    };
  }, []);

  // Load on connect
  useSocketEvent('open', () => {
    (async () => {
      // initial data load
      await fetchModdableVersions();
      await fetchSetupStatus();
      await fetchInstallationInfo();
      await fetchHostInfo();
    })();
  });

  useSocketEvent('error', event => {
    console.error('Error while attempting to connect socket', event);
  });

  useSocketEvent('close', () => {
    setSubsequentConnect(true);
    startSocket();
  });

  const theme = useMantineTheme();
  const isClosed = useIsSocketClosed() && subsequentConnect;

  // https://mantine.dev/guides/dark-theme/#save-to-localstorage-and-add-keyboard-shortcut
  const [colorScheme, setColorScheme] = useLocalStorage<ColorScheme>({
    key: 'mantine-color-scheme',
    defaultValue: 'dark',
    getInitialValueInEffect: true,
  });

  const toggleColorScheme = (value?: ColorScheme) =>
    setColorScheme(value || (colorScheme === 'dark' ? 'light' : 'dark'));

  useHotkeys([['mod+J', () => toggleColorScheme()]]);

  return (
    <ColorSchemeProvider colorScheme={colorScheme} toggleColorScheme={toggleColorScheme}>
      <MantineProvider withGlobalStyles withNormalizeCSS theme={{ colorScheme, cursorType: 'pointer' }}>
        <NotificationsProvider>
          <AppShell padding="md" header={<AppHeader />}>
            <Modal
              opened={isClosed}
              withCloseButton={false}
              onClose={() => {
                /* do nothing*/
              }}
            >
              <Stack align="center">
                <IconPlugConnectedX size={32} color={theme.colors.orange[4]} />
                <Stack align="center" spacing={2}>
                  <Title order={4}>BMBF has lost connection with your headset</Title>
                  <Text>Attempting to reconnect</Text>
                </Stack>
                <Loader variant="dots" my="xs" />
              </Stack>
            </Modal>

            <Routes>
              <Route path="/" element={<Home />} />
              <Route
                path="/mods"
                element={
                  <RequireSetup>
                    <Mods />
                  </RequireSetup>
                }
              />
              <Route
                path="/playlists"
                element={
                  <RequireSetup>
                    <Playlists />
                  </RequireSetup>
                }
              />
              <Route
                path="/songs"
                element={
                  <RequireSetup>
                    <Songs />
                  </RequireSetup>
                }
              />
              <Route
                path="/syncSaber"
                element={
                  <RequireSetup>
                    <SyncSaber />
                  </RequireSetup>
                }
              />
              <Route
                path="/tools"
                element={
                  <RequireSetup>
                    <Tools />
                  </RequireSetup>
                }
              />
              <Route
                path="/setup"
                element={
                  <RequireUnpatchedGame>
                    <Setup />
                  </RequireUnpatchedGame>
                }
              />
              <Route path="*" element={<Title>Not found</Title>} />
            </Routes>

            <ProgressIndicator />
          </AppShell>
        </NotificationsProvider>
      </MantineProvider>
    </ColorSchemeProvider>
  );
}

function RequireSetup({ children }: { children: JSX.Element }): JSX.Element {
  const location = useLocation();

  const { installationInfo } = useSnapshot(beatSaberStore);
  const { setupStatus } = useSnapshot(setupStore);

  // redirect when not installed or patched
  if (!installationInfo || !installationInfo.modTag || setupStatus) {
    if (location.pathname !== '/setup') {
      return <Navigate to="/setup" state={{ from: location }} replace />;
    }
  }

  return children;
}

function RequireUnpatchedGame({ children }: { children: JSX.Element }): JSX.Element {
  const location = useLocation();

  const { installationInfo } = useSnapshot(beatSaberStore);
  const { setupStatus } = useSnapshot(setupStore);

  // redirect when a modded version is installed
  if (!setupStatus && installationInfo && installationInfo.modTag) {
    if (location.pathname === '/setup') {
      return <Navigate to="/" state={{ from: location }} replace />;
    }
  }

  return children;
}
