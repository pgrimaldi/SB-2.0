import { ServicePeriodAvailability } from './service-period-availability';

export class HourlyServiceSelectedPrices {
  isPeriodPricingEnabled: boolean | null = null;
  periodAvailabilities: ServicePeriodAvailability[] = [];
  idSectors: string[] = [];
  isOnlinePurchaseEnabled: boolean | null = null;
}
