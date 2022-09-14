import { Link, useMatch, useResolvedPath } from 'react-router-dom';
import { ActionIcon, Button } from '@mantine/core';
import React from 'react';
import { Page } from './AppHeader';
import { useMediaQuery } from '@mantine/hooks';

interface NavigationButtonProps {
  page: Page;
  disabled?: boolean;
}

function NavigationButton({ page, ...props }: NavigationButtonProps) {
  const resolved = useResolvedPath(page.to);
  const match = useMatch({ path: resolved.pathname, end: true });

  const variant = match ? 'filled' : 'default';
  const mobile = useMediaQuery('(max-width: 1075px)');

  return (
    <Link to={page.to}>
      {mobile ? (
        <ActionIcon variant={variant} size="lg" color="blue" radius="md" {...props}>
          {page.icon}
        </ActionIcon>
      ) : (
        <Button leftIcon={page.icon} variant={variant} radius="md" {...props}>
          {page.name}
        </Button>
      )}
    </Link>
  );
}

export default NavigationButton;
