import { SeasonPeriodInterval } from './season-period-interval';

export const MAX_SEASON_PERIOD_RANGES = 20;

export class SeasonPeriodItem {
  key: number | null = null;
  name: string | null = null;
  ranges: SeasonPeriodInterval[] = [];
}
