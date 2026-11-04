import { AutomaticPriceTimeSlot } from './automatic-price-time-slot';

export class AutomaticPriceValidityPeriod {
  dateFrom: string | null = null;
  dateTo: string | null = null;
  daysOfWeek: number[] = [];
  timeSlots: AutomaticPriceTimeSlot[] = [];
}
