import { Link, useMatch, useResolvedPath } from 'react-router-dom';
import { Button, useMantineTheme } from '@mantine/core';
import React from 'react';
import { Page } from './AppHeader';
import { useMediaQuery } from '@mantine/hooks';

interface NavigationButtonProps {
  page: Page;
}

function NavigationButton({ page }: NavigationButtonProps) {
  const resolved = useResolvedPath(page.to);
  const match = useMatch({ path: resolved.pathname, end: true });

  const theme = useMantineTheme();
  const mobile = useMediaQuery(`(max-width: ${theme.breakpoints.sm}px)`, false);

  return (
    <Button
      component={Link}
      to={page.to}
      leftIcon={mobile ? undefined : page.icon}
      variant={match ? 'filled' : 'default'}
      radius="md"
    >
      {mobile ? page.icon : page.name}
    </Button>
  );
}

export default NavigationButton;
