import { Group, Header, ActionIcon } from '@mantine/core';
import { Link, useMatch, useResolvedPath } from 'react-router-dom';
import { IconHome, IconMusic, IconPlaylist, IconRefresh, IconSettings, IconTool } from '@tabler/icons';
import React from 'react';
import NavigationButton from './NavigationButton';
import { useNeedsSetup } from '../../api/beatsaber';

export interface Page {
  to: string;
  icon: React.ReactNode;
  name: string;
}

const pages: Page[] = [
  {
    to: '/mods',
    icon: <IconTool />,
    name: 'Mods',
  },
  {
    to: '/playlists',
    icon: <IconPlaylist />,
    name: 'Playlists',
  },
  {
    to: '/songs',
    icon: <IconMusic />,
    name: 'Songs',
  },
  {
    to: '/syncSaber',
    icon: <IconRefresh />,
    name: 'SyncSaber',
  },
  {
    to: '/tools',
    icon: <IconSettings />,
    name: 'Tools',
  },
];

function AppHeader() {
  const resolvedHome = useResolvedPath('/');
  const matchedHome = useMatch({ path: resolvedHome.pathname, end: true });

  const needsSetup = useNeedsSetup();

  return (
    <Header height={60}>
      <Group
        style={{
          height: '100%',
          marginTop: 0,
          marginBottom: 0,
        }}
        px="lg"
        position="center"
        align="center"
        noWrap
      >
        <Link to="/">
          <ActionIcon variant={matchedHome ? 'filled' : 'default'} color="blue" radius="xl" size="xl">
            <IconHome />
          </ActionIcon>
        </Link>
        {pages.map(page => (
          <NavigationButton key={page.to} page={page} disabled={needsSetup} />
        ))}
      </Group>
    </Header>
  );
}

export default AppHeader;