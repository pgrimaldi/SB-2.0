import { BarServiceIcon } from '../../enums/bar-service-icon';
import { StandardServiceIcon } from '../../enums/standard-service-icon';
import { AutomaticPriceValidityPeriod } from './automatic-price-validity-period';
import { ServiceTranslation } from './service-translation';

export class AutomaticPriceAddItemRequest {
  translations: ServiceTranslation[] = [];
  icon: BarServiceIcon | StandardServiceIcon | null = null;
  price: number | null = null;
  vatRate: number | null = null;
  isOnlinePurchaseEnabled = false;
  validityPeriods: AutomaticPriceValidityPeriod[] = [];
  idBarServices: string[] = [];
  idStandardServices: string[] = [];
  idHourlyServices: string[] = [];
  isPricePerServiceEnabled = false;
  idSectors: string[] = [];
}
