import { SeasonPeriodInterval } from './season-period-interval';

export class SeasonPeriodUpdateRequest {
  name: string | null = null;
  ranges: SeasonPeriodInterval[] = [];
}
