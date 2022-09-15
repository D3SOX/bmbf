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
