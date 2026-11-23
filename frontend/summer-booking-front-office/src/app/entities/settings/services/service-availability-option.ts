import { ServiceTimeSlot } from '../../enums/service-time-slot';
import { DAYS_OF_WEEK } from '../../shared/days-of-week';
import { ServiceTimeSlotPrice } from './service-time-slot-price';

export const MAX_CUSTOM_TIME_SLOT_MINUTES = 1_440;

export class ServiceAvailabilityOption {
  startTime: string | null = null;
  endTime: string | null = null;
  daysOfWeek: number[] = DAYS_OF_WEEK.map(({ number }) => number);
  /** Also the durations that are not a `ServiceTimeSlot`, after the others: sent back as read. */
  timeSlotPrices: ServiceTimeSlotPrice[] = Object.values(ServiceTimeSlot).map((timeSlot) =>
    Object.assign(new ServiceTimeSlotPrice(), { timeSlot }),
  );
  isCustomTimeSlotEnabled = false;
  customTimeSlotMinuteQuantity: number | null = null;
  customTimeSlotPrice: number | null = null;
  vatRate: number | null = null;
  maxBookingPerTimeSlotQuantity: number | null = null;
  hasFixedTimeSlots = false;
}
