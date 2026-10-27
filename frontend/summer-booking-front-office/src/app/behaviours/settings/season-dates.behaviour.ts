import { Injectable, signal } from '@angular/core';
import { SeasonDates } from '../../entities/settings/seasons/season-dates';
import { SeasonPeriodInterval } from '../../entities/settings/seasons/season-period-interval';

/**
 * Provided by the page Stagioni e periodi: the season dates as loaded or last saved, shared by the
 * panel that shows them and by the tabs, whose periods stay inside the days of their season.
 */
@Injectable()
export class SeasonDatesBehaviour {
  readonly dates = signal<SeasonDates | null>(null);

  /** First and last day of the current season, or of the next one; null while one of them is missing. */
  rangeOf(isNextSeason: boolean): SeasonPeriodInterval | null {
    const dates = this.dates();
    const dateFrom =
      (isNextSeason ? dates?.nextSeasonDateFrom : dates?.currentSeasonDateFrom) ?? null;
    const dateTo = (isNextSeason ? dates?.nextSeasonDateTo : dates?.currentSeasonDateTo) ?? null;
    return dateFrom && dateTo ? { dateFrom, dateTo } : null;
  }
}
