import { API_ROOT, backendRequest, sendErrorNotification } from './base';
import { ImportResponse, ImportType } from '../types/import';

export async function startImport(url: string): Promise<void> {
  const data = await backendRequest(`import/url`, {
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
