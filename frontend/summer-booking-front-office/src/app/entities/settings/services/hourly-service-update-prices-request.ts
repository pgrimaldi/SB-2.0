import { ServicePeriodAvailability } from './service-period-availability';

export class HourlyServiceUpdatePricesRequest {
  idHourlyService: string | null = null;
  isPeriodPricingEnabled = false;
  periodAvailabilities: ServicePeriodAvailability[] = [];
  idSectors: string[] = [];
  isOnlinePurchaseEnabled = false;
}
