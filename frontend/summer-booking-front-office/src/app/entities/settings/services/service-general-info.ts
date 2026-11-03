import { BarServiceCategory } from '../../enums/bar-service-category';
import { BarServiceIcon } from '../../enums/bar-service-icon';
import { ServiceTranslation } from './service-translation';

export class ServiceGeneralInfo {
  translations: ServiceTranslation[] = [];
  category: BarServiceCategory | null = null;
  icon: BarServiceIcon | null = null;
  isHighlighted = false;
}
