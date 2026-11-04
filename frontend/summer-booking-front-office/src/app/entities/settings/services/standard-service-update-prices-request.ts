import { ServicePeriodPrice } from './service-period-price';

export class StandardServiceUpdatePricesRequest {
  idStandardService: string | null = null;
  price: number | null = null;
  vatRate: number | null = null;
  isPeriodPricingEnabled = false;
  periodPrices: ServicePeriodPrice[] = [];
  idSectors: string[] = [];
  isOnlinePurchaseEnabled = false;
}
