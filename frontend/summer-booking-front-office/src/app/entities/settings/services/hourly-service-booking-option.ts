import { ServiceAvailabilityOption } from './service-availability-option';
import { ServiceBookingOption } from './service-booking-option';

/** Only the days and the availability are in the form: the rest is sent back as read. */
export class HourlyServiceBookingOption extends ServiceBookingOption {
  availabilityOption = new ServiceAvailabilityOption();
}
