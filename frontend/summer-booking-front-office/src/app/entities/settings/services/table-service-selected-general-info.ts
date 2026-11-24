import { ServiceTranslation } from './service-translation';

export class TableServiceSelectedGeneralInfo {
  idTableService: string | null = null;
  translations: ServiceTranslation[] = [];
  isHighlighted: boolean | null = null;
  version: string | null = null;
}
