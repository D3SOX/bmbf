import { Group, Header, ActionIcon } from '@mantine/core';
import { Link } from 'react-router-dom';
import {
  IconHandTwoFingers,
  IconHome,
  IconMusic,
  IconPlaylist,
  IconSettings,
  IconTool,
} from '@tabler/icons';
import React from 'react';

const pages: {
  path: string;
  icon: React.ReactNode;
}[] = [
  {
    path: '/mods',
    icon: <IconTool />,
  },
  {
    path: '/playlists',
    icon: <IconPlaylist />,
  },
  {
    path: '/songs',
    icon: <IconMusic />,
  },
  {
    path: '/syncSaber',
    icon: <IconHandTwoFingers />,
  },
  {
    path: '/tools',
    icon: <IconSettings />,
  },
];

function AppHeader() {
  return (
    <Header height={60} p="xs">
      <Group
        style={{
          height: '100%',
          marginTop: 0,
          marginBottom: 0,
        }}
        px="lg"
        position="apart"
        align="center"
        noWrap
      >
        <Group>
          <Link to="/">
            <ActionIcon variant="default" radius="xl" size="xl">
              <IconHome />
            </ActionIcon>
          </Link>
          {pages.map(({ path, icon }) => (
            <Link to={path} key={path}>
              <ActionIcon variant="default" radius="xl" size="xl">
                {icon}
              </ActionIcon>
            </Link>
          ))}
        </Group>
        <Group></Group>
      </Group>
    </Header>
  );
}

export default AppHeader;
