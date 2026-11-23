import { StandardServiceIcon } from '../../enums/standard-service-icon';
import { HourlyServiceBookingOption } from './hourly-service-booking-option';
import { ServiceAvailabilityOption } from './service-availability-option';
import { ServicePeriodAvailability } from './service-period-availability';
import { ServiceTranslation } from './service-translation';

export class HourlyServiceAddItemRequest {
  translations: ServiceTranslation[] = [];
  icon: StandardServiceIcon | null = null;
  isHighlighted = false;
  price: number | null = null;
  vatRate: number | null = null;
  isPeriodPricingEnabled = false;
  /** Without `isPeriodPricingEnabled`: valid in the whole season. */
  availabilityOptions: ServiceAvailabilityOption[] = [];
  /** With `isPeriodPricingEnabled`. */
  periodAvailabilities: ServicePeriodAvailability[] = [];
  idSectors: string[] = [];
  isOnlinePurchaseEnabled = false;
  bookingOptions: HourlyServiceBookingOption[] = [];
}
