export type Versions = string[];

export const enum SetupStage {
  Downgrading = 'Downgrading',
  Patching = 'Patching',
  UninstallingOriginal = 'UninstallingOriginal',
  InstallingModded = 'InstallingModded',

  Finalizing = 'Finalizing',
}

export interface DowngradingStatus {
  path: DiffInfo[];
  currentDiff: number;
}

export interface DiffInfo {
  fromVersion: string;
  toVersion: string;
  name: string | null;
}

export interface SetupStatus {
  downgradingStatus: DowngradingStatus | null;
  stage: SetupStage;
  isInProgress: boolean;
  currentBeatSaberVersion: string;
}
