import { StandardServiceIcon } from '../../enums/standard-service-icon';
import { ServiceTranslation } from './service-translation';

export class StandardServiceUpdateGeneralRequest {
  idStandardService: string | null = null;
  translations: ServiceTranslation[] = [];
  icon: StandardServiceIcon | null = null;
  isHighlighted = false;
}
