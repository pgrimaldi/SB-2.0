import { ServiceAvailabilityOption } from './service-availability-option';
import { ServiceBookingOption } from './service-booking-option';

export class HourlyServiceSelectedBookingOption extends ServiceBookingOption {
  availabilityOption: ServiceAvailabilityOption | null = null;
}
