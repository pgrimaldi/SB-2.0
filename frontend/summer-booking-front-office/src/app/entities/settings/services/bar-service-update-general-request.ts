import { BarServiceCategory } from '../../enums/bar-service-category';
import { BarServiceIcon } from '../../enums/bar-service-icon';
import { ServiceTranslation } from './service-translation';

export class BarServiceUpdateGeneralRequest {
  idBarService: string | null = null;
  translations: ServiceTranslation[] = [];
  category: BarServiceCategory | null = null;
  icon: BarServiceIcon | null = null;
  isHighlighted = false;
}
