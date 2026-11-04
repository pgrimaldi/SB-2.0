import { StandardServiceIcon } from '../../enums/standard-service-icon';
import { HourlyServiceBookingOption } from './hourly-service-booking-option';
import { ServicePeriodAvailability } from './service-period-availability';
import { ServiceTranslation } from './service-translation';

export class HourlyServiceAddItemRequest {
  translations: ServiceTranslation[] = [];
  icon: StandardServiceIcon | null = null;
  isHighlighted = false;
  isPeriodPricingEnabled = false;
  periodAvailabilities: ServicePeriodAvailability[] = [];
  idSectors: string[] = [];
  isOnlinePurchaseEnabled = false;
  bookingOptions: HourlyServiceBookingOption[] = [];
}
