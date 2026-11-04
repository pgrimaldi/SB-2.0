import { VoucherTimeSlot } from './voucher-time-slot';

/** ISO 8601 numbers: 1 is Monday, 7 is Sunday. */
export const DAYS_OF_WEEK = [
  { number: 1, key: 'monday' },
  { number: 2, key: 'tuesday' },
  { number: 3, key: 'wednesday' },
  { number: 4, key: 'thursday' },
  { number: 5, key: 'friday' },
  { number: 6, key: 'saturday' },
  { number: 7, key: 'sunday' },
] as const;

export class VoucherValidityPeriod {
  validFrom: string | null = null;
  validTo: string | null = null;
  daysOfWeek: number[] = [];
  timeSlots: VoucherTimeSlot[] = [];
}
