import { HourlyServiceBookingOption } from './hourly-service-booking-option';
import { ServiceAvailabilityOptionDraft } from './service-availability-option-draft';

export class HourlyServiceBookingOptionDraft extends HourlyServiceBookingOption {
  override availabilityOption = new ServiceAvailabilityOptionDraft();
}
