export const MAX_BOOKING_LIMIT_DAYS = 366;

export class BookingLimitRule {
  id: string | null = null;
  sectorId: string | null = null;
  startsOn: string | null = null;
  endsOn: string | null = null;
  minDays: number | null = null;
  maxDays: number | null = null;
}
