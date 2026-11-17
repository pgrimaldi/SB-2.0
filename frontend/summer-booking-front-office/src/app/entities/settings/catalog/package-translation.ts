import { Language } from '../../shared/language';

export const MAX_PACKAGE_NAME_LENGTH = 255;

export class PackageTranslation {
  language: Language | null = null;
  packageName: string | null = null;
}
