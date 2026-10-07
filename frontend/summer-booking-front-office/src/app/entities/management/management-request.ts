import { BookingDayType } from '../enums/booking-day-type';

/**
 * Dates are ISO 8601 in UTC, and the chosen days are UTC days, with no time zone conversion:
 * 29/09 → `2026-09-29T00:00:00.000Z` … `2026-09-29T23:59:59.999Z`.
 */
export interface ManagementRequest {
  idProperty: string;
  datetimeFrom: string;
  datetimeTo: string;
  bookingDayType: BookingDayType;
}
