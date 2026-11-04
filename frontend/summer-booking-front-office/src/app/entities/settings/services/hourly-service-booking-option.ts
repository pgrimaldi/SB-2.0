import { ServiceAvailabilityOption } from './service-availability-option';

export class HourlyServiceBookingOption {
  dateFrom: string | null = null;
  dateTo: string | null = null;
  availabilityOption = new ServiceAvailabilityOption();
}
