import { ServiceTranslation } from './service-translation';

export class AutomaticPriceUpdateGeneralRequest {
  idAutomaticPrice: string | null = null;
  translations: ServiceTranslation[] = [];
  /** Also the one read that is not one of `BarServiceIcon` and `StandardServiceIcon`. */
  icon: string | null = null;
}
