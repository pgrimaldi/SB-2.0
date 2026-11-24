import { ServiceAvailabilityOption } from './service-availability-option';
import { ServicePeriodAvailability } from './service-period-availability';

export class HourlyServiceUpdatePricesRequest {
  idHourlyService: string | null = null;
  price: number | null = null;
  vatRate: number | null = null;
  isPeriodPricingEnabled = false;
  availabilityOptions: ServiceAvailabilityOption[] = [];
  periodAvailabilities: ServicePeriodAvailability[] = [];
  idSectors: string[] = [];
  isOnlinePurchaseEnabled = false;
}
