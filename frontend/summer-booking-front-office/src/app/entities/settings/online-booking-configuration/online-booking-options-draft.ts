import { OnlineBookingOptionsUpdateRequest } from './online-booking-options-update-request';

/** The options in the form: the conversion into an afternoon booking has its own switch. */
export class OnlineBookingOptionsDraft extends OnlineBookingOptionsUpdateRequest {
  isAfternoonConversionEnabled = false;
}
