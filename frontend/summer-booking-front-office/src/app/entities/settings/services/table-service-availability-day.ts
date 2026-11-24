import { TableServiceTimeSlot } from './table-service-time-slot';

export class TableServiceAvailabilityDay {
  dayOfWeek: number | null = null;
  isEnabled = false;
  startTime: string | null = null;
  endTime: string | null = null;
  /** Not in the form: sent back as read. */
  otherTimeSlots: TableServiceTimeSlot[] = [];
}
