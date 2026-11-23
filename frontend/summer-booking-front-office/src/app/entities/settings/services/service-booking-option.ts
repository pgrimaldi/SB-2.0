import { Weekday } from '../../enums/weekday';

export const MAX_SERVICE_BOOKING_QUANTITY = 32_767;

export class ServiceBookingOption {
  dateFrom: string | null = null;
  dateTo: string | null = null;
  /** Not in the form: sent back as read. */
  weekdays: Weekday[] = [];
  managementStartTime: string | null = null;
  managementEndTime: string | null = null;
  onlineStartTime: string | null = null;
  onlineEndTime: string | null = null;
  maxBookingPerHourQuantity: number | null = null;
  maxBookingPerDayQuantity: number | null = null;
  /** Not in the form: sent back as read. */
  maxPerSlot: number | null = null;
  /** Also a value that is not one of `BookingTimeInterval`, kept until another is chosen. */
  intervalMinutes: number | null = null;
  minItemPerBookingQuantity: number | null = null;
  maxItemPerBookingQuantity: number | null = null;
}
