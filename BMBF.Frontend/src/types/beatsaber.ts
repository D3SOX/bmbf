export interface InstallationInfo {
  version: string;
  versionCode: number;
  modTag: ModTag | null;
}

export interface ModTag {
  patcherName: string;
  patcherVersion: string | null;
  modloaderName: string | null;
  modloaderVersion: string | null;
  modifiedFiles: string[];
}
