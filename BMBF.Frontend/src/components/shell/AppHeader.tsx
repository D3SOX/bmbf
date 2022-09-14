import {
  Group,
  Header,
  ActionIcon,
  Button,
  useMantineColorScheme,
  Switch,
  useMantineTheme,
} from '@mantine/core';
import { Link, useMatch, useResolvedPath } from 'react-router-dom';
import {
  IconHome,
  IconMoonStars,
  IconMusic,
  IconPlayerPlay,
  IconPlaylist,
  IconRefresh,
  IconSettings,
  IconSun,
  IconTool,
} from '@tabler/icons';
import React from 'react';
import NavigationButton from './NavigationButton';
import { launchBeatSaber, useNeedsSetup } from '../../api/beatsaber';
import { useMediaQuery } from '@mantine/hooks';

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

  const theme = useMantineTheme();
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();
  const dark = colorScheme === 'dark';

  const useIconButton = useMediaQuery('(max-width: 1075px)');

  return (
    <Header height={60}>
      <Group
        style={{
          height: '100%',
        }}
        px="lg"
        noWrap
      >
        <Switch
          size="xl"
          color={dark ? 'gray' : 'dark'}
          checked={!dark}
          onChange={() => toggleColorScheme()}
          onLabel={<IconSun size={16} stroke={2.5} color={theme.colors.yellow[4]} />}
          offLabel={<IconMoonStars size={16} stroke={2.5} color={theme.colors.blue[6]} />}
          sx={{ marginRight: 'auto' }}
        />
        <Link to="/">
          <ActionIcon variant={matchedHome ? 'filled' : 'default'} color="blue" radius="xl" size="xl">
            <IconHome />
          </ActionIcon>
        </Link>
        {pages.map(page => (
          <NavigationButton key={page.to} page={page} disabled={needsSetup} />
        ))}
        {useIconButton ? (
          <ActionIcon
            variant="filled"
            color="blue"
            size="lg"
            disabled={needsSetup}
            onClick={() => launchBeatSaber()}
            sx={{ marginLeft: 'auto' }}
          >
            <IconPlayerPlay />
          </ActionIcon>
        ) : (
          <Button
            leftIcon={<IconPlayerPlay />}
            variant="filled"
            disabled={needsSetup}
            onClick={() => launchBeatSaber()}
            sx={{ marginLeft: 'auto' }}
          >
            Start Beat Saber
          </Button>
        )}
      </Group>
    </Header>
  );
}

export default AppHeader;
