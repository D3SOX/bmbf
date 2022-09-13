export const enum CoreModResultType {
  UsedDownloaded = 'UsedDownloaded',
  UsedBuiltIn = 'UsedBuiltIn',
  NoneAvailableForVersion = 'NoneAvailableForVersion',
  NoneBuiltInForVersion = 'NoneBuiltInForVersion',
  FailedToFetch = 'FailedToFetch',
  BeatSaberNotInstalled = 'BeatSaberNotInstalled',
}

export interface Mod {
  id: string;
  name: string;
  author: string;
  porter: string | null;
  dependencies: Record<string, string>;
  version: string;
  description: string;
  packageVersion: string;
  coverImageFileName: string | null;
  installed: boolean;
  copyExtensions: Record<string, string>;
}

export interface CoreMod {
  id: string;
  version: string;
  downloadLink: string;
  filename: string;
}

export interface CoreModInstallResult {
  added: CoreMod[];
  installed: CoreMod[];
  failedToFetch: CoreMod[];
  failedToInstall: CoreMod[];
  resultType: CoreModResultType;
}
