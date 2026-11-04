import { HourlyServiceBookingOption } from './hourly-service-booking-option';

export class HourlyServiceUpdateBookingRequest {
  idHourlyService: string | null = null;
  bookingOptions: HourlyServiceBookingOption[] = [];
}
