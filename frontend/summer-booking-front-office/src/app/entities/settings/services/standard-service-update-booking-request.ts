import { ServiceBookingOption } from './service-booking-option';

export class StandardServiceUpdateBookingRequest {
  idStandardService: string | null = null;
  bookingOptions: ServiceBookingOption[] = [];
}
