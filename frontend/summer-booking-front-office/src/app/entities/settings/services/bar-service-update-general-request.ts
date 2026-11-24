import { BarServiceCategory } from '../../enums/bar-service-category';
import { ServiceTranslation } from './service-translation';

export class BarServiceUpdateGeneralRequest {
  idBarService: string | null = null;
  translations: ServiceTranslation[] = [];
  category: BarServiceCategory | null = null;
  icon: string | null = null;
  isHighlighted = false;
}
