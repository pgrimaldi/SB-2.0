import { BarServiceIcon } from '../../enums/bar-service-icon';
import { StandardServiceIcon } from '../../enums/standard-service-icon';
import { ServiceTranslation } from './service-translation';

export class AutomaticPriceAddItemRequest {
  translations: ServiceTranslation[] = [];
  icon: BarServiceIcon | StandardServiceIcon | null = null;
}
