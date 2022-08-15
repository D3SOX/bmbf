import { AppShell, MantineProvider, Title } from '@mantine/core';
import AppHeader from './components/shell/AppHeader';
import { Route, Routes } from 'react-router-dom';
import Home from './pages/Home';
import Mods from './pages/Mods';
import Playlists from './pages/Playlists';
import Songs from './pages/Songs';
import SyncSaber from './pages/SyncSaber';
import Tools from './pages/Tools';

export default function App() {
  return (
    <MantineProvider withGlobalStyles withNormalizeCSS theme={{ colorScheme: 'dark' }}>
      <AppShell padding="md" header={<AppHeader />}>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/mods" element={<Mods />} />
          <Route path="/playlists" element={<Playlists />} />
          <Route path="/songs" element={<Songs />} />
          <Route path="/syncSaber" element={<SyncSaber />} />
          <Route path="/tools" element={<Tools />} />
          <Route path="*" element={<Title>Not found</Title>} />
        </Routes>
      </AppShell>
    </MantineProvider>
  );
}
