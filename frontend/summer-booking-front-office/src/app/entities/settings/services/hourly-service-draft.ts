import { HourlyServiceAddItemRequest } from './hourly-service-add-item-request';
import { HourlyServiceBookingOptionDraft } from './hourly-service-booking-option-draft';
import { ServiceAvailabilityOptionDraft } from './service-availability-option-draft';
import { ServicePeriodAvailabilityDraft } from './service-period-availability-draft';

/** An hourly service in the form, also with the availabilities left empty: they are not sent. */
export class HourlyServiceDraft extends HourlyServiceAddItemRequest {
  override availabilityOptions: ServiceAvailabilityOptionDraft[] = [];
  override periodAvailabilities: ServicePeriodAvailabilityDraft[] = [];
  override bookingOptions: HourlyServiceBookingOptionDraft[] = [];
}
