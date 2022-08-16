import { Stack, Title, Text } from '@mantine/core';
import { generateAcronym } from '../utils';

function Home() {
  const acronym = generateAcronym();

  return (
    <Stack align="center">
      <img src="/logo.png" alt="Logo" />
      <Title>Home</Title>
      <Text>Welcome to BMBF! ({acronym})</Text>
    </Stack>
  );
}

export default Home;
