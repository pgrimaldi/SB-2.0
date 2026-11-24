import { ServiceTranslation } from './service-translation';

export class StandardServiceUpdateGeneralRequest {
  idStandardService: string | null = null;
  translations: ServiceTranslation[] = [];
  icon: string | null = null;
  isHighlighted = false;
}
