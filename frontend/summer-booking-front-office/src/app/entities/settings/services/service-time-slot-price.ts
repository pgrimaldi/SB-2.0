import { ServiceTimeSlot } from '../../enums/service-time-slot';

export class ServiceTimeSlotPrice {
  timeSlot: ServiceTimeSlot | null = null;
  isEnabled = false;
  price: number | null = null;
}
