import { ServiceTimeSlot } from '../../enums/service-time-slot';
import { ServiceTimeSlotPrice } from './service-time-slot-price';

export class ServiceAvailabilityOption {
  startTime: string | null = null;
  endTime: string | null = null;
  daysOfWeek: number[] = [];
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
