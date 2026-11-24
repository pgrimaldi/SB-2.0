import { TableServiceAvailabilityDay } from './table-service-availability-day';

export class TableServiceAvailability {
  dateFrom: string | null = null;
  dateTo: string | null = null;
  days: TableServiceAvailabilityDay[] = [];
}
