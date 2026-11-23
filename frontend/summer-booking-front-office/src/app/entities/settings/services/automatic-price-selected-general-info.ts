import { ServiceTranslation } from './service-translation';

export class AutomaticPriceSelectedGeneralInfo {
  idAutomaticPrice: string | null = null;
  translations: ServiceTranslation[] = [];
  /** Also an icon that is not one of `BarServiceIcon` and `StandardServiceIcon`. */
  icon: string | null = null;
  imageUrl: string | null = null;
  version: string | null = null;
}
