export interface Mod {
  id: string;
  name: string;
  author: string;
  porter: string | null;
  version: string;
  description: string;
  packageVersion: string;
  coverImageFileName: string;
  installed: boolean;
  copyExtensions: Record<string, string>;
}
