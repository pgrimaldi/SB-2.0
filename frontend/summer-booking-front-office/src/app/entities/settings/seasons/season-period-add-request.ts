import { SeasonPeriodInterval } from './season-period-interval';

export class SeasonPeriodAddRequest {
  name: string | null = null;
  ranges: SeasonPeriodInterval[] = [];
}
