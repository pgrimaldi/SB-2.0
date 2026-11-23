import { ServiceTranslation } from './service-translation';

export class HourlyServiceUpdateGeneralRequest {
  idHourlyService: string | null = null;
  translations: ServiceTranslation[] = [];
  icon: string | null = null;
  isHighlighted = false;
}
