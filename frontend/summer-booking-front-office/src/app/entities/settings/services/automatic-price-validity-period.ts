import { DAYS_OF_WEEK } from '../../shared/days-of-week';
import { AutomaticPriceTimeSlot } from './automatic-price-time-slot';

export class AutomaticPriceValidityPeriod {
  dateFrom: string | null = null;
  dateTo: string | null = null;
  daysOfWeek: number[] = DAYS_OF_WEEK.map(({ number }) => number);
  timeSlots: AutomaticPriceTimeSlot[] = [];
}
