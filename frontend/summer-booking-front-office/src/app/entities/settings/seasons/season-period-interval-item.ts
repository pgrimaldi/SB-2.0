/** Intervals of one bulk delete at most, as the backend (`BulkResult.MaxIds`). */
export const MAX_BULK_DELETE_RANGES = 300;

export class SeasonPeriodIntervalItem {
  key: number | null = null;
  startsOn: string | null = null;
  endsOn: string | null = null;
}
