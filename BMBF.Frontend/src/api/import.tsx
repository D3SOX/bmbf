import { backendRequest, sendErrorNotification } from './base';
import { ImportResponse, ImportType } from '../types/import';

export async function importViaUrl(url: string): Promise<void> {
  const data = await backendRequest('import/url', {
    method: 'POST',
    body: `"${url}"`,
  });
  if (data.ok) {
    const json: ImportResponse = await data.json();
    if (json.type === ImportType.Failed) {
      sendErrorNotification(json.error);
    }
  }
}

export async function importViaFile(file: File): Promise<void> {
  if (file.name.endsWith('.zip') || file.name.endsWith('.qmod') || file.name.endsWith('.bplist')) {
    const data = await backendRequest('import/file', {
      method: 'POST',
      body: file,
      headers: {
        'Content-Disposition': `attachment; filename="${file.name}"`,
      },
    });
    if (data.ok) {
      const json: ImportResponse = await data.json();
      if (json.type === ImportType.Failed) {
        sendErrorNotification(json.error);
      }
    }
  } else {
    sendErrorNotification(`Invalid file type for "${file.name}"`);
  }
}
