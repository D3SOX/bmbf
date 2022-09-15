import { showNotification } from '@mantine/notifications';
import { IconAlertTriangle, IconCheck } from '@tabler/icons';

export const API_HOST = `${window.location.hostname}:50005`;
export const API_ROOT = `http://${API_HOST}/api`;

export function sendSuccessNotification(message: string) {
  showNotification({
    autoClose: 2000,
    title: 'Success',
    message: message || 'Successfully executed action',
    color: 'green',
    icon: <IconCheck />,
  });
}

export function sendErrorNotification(error: string) {
  showNotification({
    autoClose: 2000,
    title: 'Error',
    message: error || 'An unknown error occurred',
    color: 'red',
    icon: <IconAlertTriangle />,
  });
}

export async function backendRequest(
  ...args: [...Parameters<typeof fetch>, number[]?]
): Promise<Response> {
  try {
    const ignoredCodes = args[2];
    const result = await fetch(`${API_ROOT}/${args[0]}`, args[1]);

    if (result.ok || ignoredCodes?.some(a => a === result.status)) return result;

    console.error('Request failed', result);
    sendErrorNotification(
      `Error while making request, received ${result.status}\n${await result.text()}`
    );

    return result;
  } catch (e) {
    console.error('Error while fulfilling request', e);
    sendErrorNotification(`Error while making request`);
    throw e;
  }
}

export async function quit(): Promise<void> {
  await backendRequest('quit', {
    method: 'POST',
  });
}

export async function restart(): Promise<void> {
  await backendRequest('restart', {
    method: 'POST',
  });
}

export async function getRunInBackground(): Promise<boolean> {
  const data = await backendRequest('runInBackground');
  if (data.ok) {
    return (await data.text()) === 'true';
  }
  return false;
}

export async function setRunInBackground(state: boolean): Promise<void> {
  await backendRequest('runInBackground', {
    method: 'POST',
    body: `${state}`,
  });
}

export async function logs(): Promise<void> {
  const data = await backendRequest('logs');
  if (data.ok) {
    const logText = await data.text();
    const element = document.createElement('a');
    element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(logText));
    element.setAttribute('download', 'logs.txt');
    element.style.display = 'none';
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
  }
}
