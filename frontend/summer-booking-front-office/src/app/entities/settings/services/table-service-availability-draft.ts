import { TableServiceAvailability } from './table-service-availability';

/** An availability in the form, with the hours for all its days: not sent, they are those of the days. */
export class TableServiceAvailabilityDraft extends TableServiceAvailability {
  isSameTimeForAllDays = false;
  startTime: string | null = null;
  endTime: string | null = null;
}
