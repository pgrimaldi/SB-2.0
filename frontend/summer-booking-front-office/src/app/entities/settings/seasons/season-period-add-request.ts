import { SeasonPeriodInterval } from './season-period-interval';

export class SeasonPeriodAddRequest {
  periodName: string | null = null;
  intervals: SeasonPeriodInterval[] = [];
}
