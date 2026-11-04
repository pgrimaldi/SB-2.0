import { StandardServiceIcon } from '../../enums/standard-service-icon';
import { ServiceTranslation } from './service-translation';

export class HourlyServiceSelectedGeneralInfo {
  idHourlyService: string | null = null;
  translations: ServiceTranslation[] = [];
  icon: StandardServiceIcon | null = null;
  isHighlighted: boolean | null = null;
}
