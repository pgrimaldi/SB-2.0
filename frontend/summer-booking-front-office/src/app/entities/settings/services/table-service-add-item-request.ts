import { ServiceTranslation } from './service-translation';

export class TableServiceAddItemRequest {
  translations: ServiceTranslation[] = [];
  isHighlighted = false;
}
