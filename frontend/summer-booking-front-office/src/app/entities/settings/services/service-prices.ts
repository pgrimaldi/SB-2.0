import { ServicePeriodPrice } from './service-period-price';
import { ServiceVariant } from './service-variant';

export class ServicePrices {
  price: number | null = null;
  vatRate: number | null = null;
  variants: ServiceVariant[] = [];
  isOnlinePurchaseEnabled = false;
  isPeriodPricingEnabled = false;
  periodPrices: ServicePeriodPrice[] = [];
  idSectors: string[] = [];
}
