import { TableServiceAvailabilityDay } from './table-service-availability-day';

export class TableServiceAvailability {
  dateFrom: string | null = null;
  dateTo: string | null = null;
  isSameTimeForAllDays = false;
  startTime: string | null = null;
  endTime: string | null = null;
  days: TableServiceAvailabilityDay[] = [];
}
