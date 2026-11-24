import { ServiceAvailabilityOption } from './service-availability-option';
import { ServicePeriodAvailability } from './service-period-availability';

export class HourlyServiceSelectedPrices {
  price: number | null = null;
  vatRate: number | null = null;
  isPeriodPricingEnabled: boolean | null = null;
  availabilityOptions: ServiceAvailabilityOption[] = [];
  periodAvailabilities: ServicePeriodAvailability[] = [];
  idSectors: string[] = [];
  isOnlinePurchaseEnabled: boolean | null = null;
  version: string | null = null;
}
