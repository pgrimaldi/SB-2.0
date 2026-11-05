import { ServiceTranslation } from './service-translation';

export class TableServiceUpdateGeneralRequest {
  idTableService: string | null = null;
  translations: ServiceTranslation[] = [];
  isHighlighted = false;
}
