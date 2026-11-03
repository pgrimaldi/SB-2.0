import { ServiceTranslation } from './service-translation';

export class ServiceGeneralInfo {
  translations: ServiceTranslation[] = [];
  idCategory: string | null = null;
  idIcon: number | null = null;
  isHighlighted = false;
}
