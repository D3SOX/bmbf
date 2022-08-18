import { API_ROOT, sendErrorNotification } from './base';
import { ImportResponse, ImportType } from '../types/import';

export async function startImport(url: string): Promise<void> {
  const data = await fetch(`${API_ROOT}/import/url`, {
    method: 'POST',
    body: `"${url}"`,
  });
  if (data.ok) {
    const json: ImportResponse = await data.json();
    if (json.type === ImportType.Failed) {
      sendErrorNotification(json.error);
    }
  } else {
    sendErrorNotification(await data.text());
  }
}
