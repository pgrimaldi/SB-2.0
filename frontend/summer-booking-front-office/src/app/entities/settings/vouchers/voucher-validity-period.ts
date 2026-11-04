import { VoucherTimeSlot } from './voucher-time-slot';

export class VoucherValidityPeriod {
  validFrom: string | null = null;
  validTo: string | null = null;
  daysOfWeek: number[] = [];
  timeSlots: VoucherTimeSlot[] = [];
}
