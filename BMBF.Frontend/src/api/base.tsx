import { showNotification } from '@mantine/notifications';
import { IconAlertTriangle } from '@tabler/icons';

export const API_ROOT = 'http://127.0.0.1:50006/api';

export function sendErrorNotification(error: string) {
  showNotification({
    autoClose: 2000,
    title: 'Error',
    message: error || 'An unknown error occurred',
    color: 'red',
    icon: <IconAlertTriangle />,
  });
}
