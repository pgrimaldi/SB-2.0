import { Language } from '../../shared/language';

export const MAX_SERVICE_NAME_LENGTH = 255;
export const MAX_SERVICE_DESCRIPTION_LENGTH = 10_000;

export class ServiceTranslation {
  language: Language | null = null;
  serviceName: string | null = null;
  description: string | null = null;
}
