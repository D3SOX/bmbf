import { Stack, Title, Text, Card, Button } from '@mantine/core';
import { generateAcronym } from '../utils';
import { useNeedsSetup } from '../api/beatsaber';
import { Link } from 'react-router-dom';
import { IconArrowBigRight } from '@tabler/icons';

function Home() {
  const acronym = generateAcronym();

  const needsSetup = useNeedsSetup();

  return (
    <Stack align="center">
      <img src="/logo.png" alt="Logo" />
      <Title>Home</Title>
      <Text>Welcome to BMBF! ({acronym})</Text>
      {needsSetup ? (
        <Card radius="md" shadow="md">
          <Stack>
            <Text>You need to setup BMBF before you can use it.</Text>
            <Button component={Link} to="/setup" leftIcon={<IconArrowBigRight />}>
              Setup now
            </Button>
          </Stack>
        </Card>
      ) : (
        <Text>You are all set up!</Text>
      )}
    </Stack>
  );
}

export default Home;
