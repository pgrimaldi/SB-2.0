import { ServicePeriodPrice } from './service-period-price';

export class StandardServiceSelectedPrices {
  price: number | null = null;
  vatRate: number | null = null;
  isPeriodPricingEnabled: boolean | null = null;
  periodPrices: ServicePeriodPrice[] = [];
  idSectors: string[] = [];
  isOnlinePurchaseEnabled: boolean | null = null;
}
