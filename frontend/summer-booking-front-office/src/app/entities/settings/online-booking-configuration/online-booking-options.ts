import { OnlineBookingOptionsUpdateRequest } from './online-booking-options-update-request';

export class OnlineBookingOptions extends OnlineBookingOptionsUpdateRequest {
  isSuspendedByPlatform = false;
  version: string | null = null;
}
