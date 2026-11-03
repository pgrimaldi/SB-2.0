import { ServiceBookingOption } from './service-booking-option';

export class BarServiceUpdateBookingRequest {
  idBarService: string | null = null;
  bookingOptions: ServiceBookingOption[] = [];
}
