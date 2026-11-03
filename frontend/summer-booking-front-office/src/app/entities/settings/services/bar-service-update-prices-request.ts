import { ServicePeriodPrice } from './service-period-price';
import { ServiceVariant } from './service-variant';

export class BarServiceUpdatePricesRequest {
  idBarService: string | null = null;
  price: number | null = null;
  vatRate: number | null = null;
  variants: ServiceVariant[] = [];
  isPeriodPricingEnabled = false;
  periodPrices: ServicePeriodPrice[] = [];
  idSectors: string[] = [];
  isOnlinePurchaseEnabled = false;
}
