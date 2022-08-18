import { Link, useMatch, useResolvedPath } from 'react-router-dom';
import { Button, ButtonProps, useMantineTheme } from '@mantine/core';
import React from 'react';
import { Page } from './AppHeader';
import { useMediaQuery } from '@mantine/hooks';

interface NavigationButtonProps extends ButtonProps {
  page: Page;
}

function NavigationButton({ page, ...props }: NavigationButtonProps) {
  const resolved = useResolvedPath(page.to);
  const match = useMatch({ path: resolved.pathname, end: true });

  const theme = useMantineTheme();
  const mobile = useMediaQuery(`(max-width: ${theme.breakpoints.sm}px)`, false);

  return (
    <Link to={page.to}>
      <Button
        leftIcon={mobile ? undefined : page.icon}
        variant={match ? 'filled' : 'default'}
        radius="md"
        {...props}
      >
        {mobile ? page.icon : page.name}
      </Button>
    </Link>
  );
}

export default NavigationButton;
