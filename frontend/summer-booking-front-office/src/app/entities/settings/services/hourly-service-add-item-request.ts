import { StandardServiceIcon } from '../../enums/standard-service-icon';
import { ServiceTranslation } from './service-translation';

export class HourlyServiceAddItemRequest {
  translations: ServiceTranslation[] = [];
  icon: StandardServiceIcon | null = null;
  isHighlighted = false;
}
