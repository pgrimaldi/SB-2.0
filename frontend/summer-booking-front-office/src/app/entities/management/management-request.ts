import { BookingDayType } from '../enums/booking-day-type';

export interface ManagementRequest {
  idProperty: string;
  datetimeFrom: string;
  datetimeTo: string;
  bookingDayType: BookingDayType;
}
