import { showNotification } from '@mantine/notifications';
import { IconAlertTriangle, IconCheck } from '@tabler/icons';

export const API_HOST = import.meta.env.DEV ? '127.0.0.1:50006' : '127.0.0.1:50005';
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
