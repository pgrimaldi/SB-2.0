import { ServicePeriodPrice } from './service-period-price';
import { ServiceVariant } from './service-variant';

export class BarServiceSelectedPrices {
  price: number | null = null;
  vatRate: number | null = null;
  variants: ServiceVariant[] = [];
  isPeriodPricingEnabled: boolean | null = null;
  periodPrices: ServicePeriodPrice[] = [];
  idSectors: string[] = [];
  isOnlinePurchaseEnabled: boolean | null = null;
}
