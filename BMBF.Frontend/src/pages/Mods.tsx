import { Grid, Stack, Text, Title } from '@mantine/core';
import { useSnapshot } from 'valtio';
import { fetchMods, modsStore } from '../api/mods';
import { useEffect } from 'react';
import ModCard from '../components/ModCard';

function Mods() {
  const { mods } = useSnapshot(modsStore);

  useEffect(() => {
    fetchMods();
  }, []);

  return (
    <Stack align="center">
      <img src="/logo.png" alt="Logo" />
      <Title>Mods</Title>
      {mods.length ? (
        <Grid gutter="md" grow>
          {mods.map(mod => (
            <Grid.Col key={mod.id} md={6} lg={4}>
              <ModCard mod={mod} />
            </Grid.Col>
          ))}
        </Grid>
      ) : (
        <Text>No mods found</Text>
      )}
    </Stack>
  );
}

export default Mods;
