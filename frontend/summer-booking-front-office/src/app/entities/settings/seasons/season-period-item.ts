import { SeasonPeriodIntervalItem } from './season-period-interval-item';

export class SeasonPeriodItem {
  idPeriod: string | null = null;
  periodName: string | null = null;
  intervals: SeasonPeriodIntervalItem[] = [];
}
