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
  /** The dates were read: while they are read, or if that fails, nothing starts from them. */
  readonly isLoaded = signal(false);
  /** Asks the panel to read the dates again, e.g. after a period refused as outside them. */
  readonly reloads = signal(0);

  reload(): void {
    this.reloads.update((count) => count + 1);
  }

  /** First and last day of the current season, or of the next one; null while one of them is missing. */
  rangeOf(isNextSeason: boolean): SeasonPeriodInterval | null {
    const dates = this.dates();
    const startsOn = (isNextSeason ? dates?.nextStartsOn : dates?.startsOn) ?? null;
    const endsOn = (isNextSeason ? dates?.nextEndsOn : dates?.endsOn) ?? null;
    return startsOn && endsOn ? { startsOn, endsOn } : null;
  }
}
