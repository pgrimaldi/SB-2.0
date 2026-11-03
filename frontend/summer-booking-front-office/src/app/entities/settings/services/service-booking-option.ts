import { BookingTimeInterval } from '../../enums/booking-time-interval';

export class ServiceBookingOption {
  dateFrom: string | null = null;
  dateTo: string | null = null;
  managementStartTime: string | null = null;
  managementEndTime: string | null = null;
  onlineStartTime: string | null = null;
  onlineEndTime: string | null = null;
  maxBookingPerHourQuantity: number | null = null;
  maxBookingPerDayQuantity: number | null = null;
  timeInterval: BookingTimeInterval | null = null;
  minItemPerBookingQuantity: number | null = null;
  maxItemPerBookingQuantity: number | null = null;
}
