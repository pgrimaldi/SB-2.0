import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SeasonDates } from '../../../entities/settings/seasons/season-dates';
import { SeasonPeriodAddRequest } from '../../../entities/settings/seasons/season-period-add-request';
import { SeasonPeriodInterval } from '../../../entities/settings/seasons/season-period-interval';
import { SeasonPeriodIntervalItem } from '../../../entities/settings/seasons/season-period-interval-item';
import { SeasonPeriodItem } from '../../../entities/settings/seasons/season-period-item';
import { SeasonPeriodUpdateRequest } from '../../../entities/settings/seasons/season-period-update-request';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class SeasonService {
  private readonly httpClient = inject(HttpClient);

  /** For the property of the token. Answers 204 with no body while the season is not set. */
  readonly seasonDates = (): Observable<SeasonDates> =>
    this.httpClient
      .get<SeasonDates | null>(`${environment.apiBaseUrl}/catalog/season`)
      .pipe(map((answer) => seasonDatesOf(answer)));

  /** All four days, for the property of the token. Answers 200 with the days as saved. */
  readonly saveSeasonDates = (dates: SeasonDates): Observable<SeasonDates> =>
    this.httpClient
      .put<SeasonDates>(`${environment.apiBaseUrl}/catalog/season`, daysOf(dates))
      .pipe(map((answer) => seasonDatesOf(answer)));

  /** The tariff periods of the current season, or of the next one, of the property of the token. */
  readonly periodList = (isNextSeason: boolean): Observable<SeasonPeriodItem[]> =>
    this.httpClient
      .get<SeasonPeriodItem[] | null>(periodsEndpoint(isNextSeason))
      .pipe(map((answer) => (Array.isArray(answer) ? answer.map(periodOf) : [])));

  /** A period of the current season, or of the next one. Answers 201 with the period created. */
  readonly addPeriod = (
    isNextSeason: boolean,
    request: SeasonPeriodAddRequest,
  ): Observable<SeasonPeriodItem> =>
    this.httpClient
      .post<SeasonPeriodItem>(periodsEndpoint(isNextSeason), {
        ...onlyFieldsOf(SeasonPeriodAddRequest, request),
        ranges: request.ranges.map(rangeDaysOf),
      })
      .pipe(map(periodOf));

  /** The period with all its date intervals. Answers 204 with no body. */
  readonly deletePeriod = (isNextSeason: boolean, key: number): Observable<void> =>
    this.httpClient.delete<void>(`${periodsEndpoint(isNextSeason)}/${key}`);

  /** Name and all the date intervals of the period, the ones it keeps too. Answers 200 with the period saved. */
  readonly updatePeriod = (
    isNextSeason: boolean,
    key: number,
    request: SeasonPeriodUpdateRequest,
  ): Observable<SeasonPeriodItem> =>
    this.httpClient
      .put<SeasonPeriodItem>(`${periodsEndpoint(isNextSeason)}/${key}`, {
        ...onlyFieldsOf(SeasonPeriodUpdateRequest, request),
        ranges: request.ranges.map(rangeDaysOf),
      })
      .pipe(map(periodOf));

  /** Date intervals of one or more periods of the season, all or none. Answers 204 with no body. */
  readonly deleteRanges = (
    isNextSeason: boolean,
    ranges: readonly SeasonPeriodIntervalItem[],
  ): Observable<void> =>
    this.httpClient.post<void>(`${periodsEndpoint(isNextSeason)}/bulk-delete-ranges`, {
      ranges: ranges.map((range) => ({
        ...onlyFieldsOf(SeasonPeriodIntervalItem, range),
        ...rangeDaysOf(range),
      })),
    });
}

/** The backend sends and takes days (`2026-05-01`); the page keeps them at midnight UTC. */
function seasonDatesOf(answer: SeasonDates | null): SeasonDates {
  const days = onlyFieldsOf(SeasonDates, answer ?? new SeasonDates());
  return {
    startsOn: midnightOf(days.startsOn),
    endsOn: midnightOf(days.endsOn),
    nextStartsOn: midnightOf(days.nextStartsOn),
    nextEndsOn: midnightOf(days.nextEndsOn),
  };
}

function periodsEndpoint(isNextSeason: boolean): string {
  return `${environment.apiBaseUrl}/catalog/${isNextSeason ? 'next-season/' : ''}tariff-periods`;
}

function periodOf(answer: SeasonPeriodItem): SeasonPeriodItem {
  const period = onlyFieldsOf(SeasonPeriodItem, answer);
  const ranges = Array.isArray(period.ranges) ? period.ranges : [];
  return {
    ...period,
    ranges: ranges.map((range) => {
      const days = onlyFieldsOf(SeasonPeriodInterval, range);
      return { startsOn: midnightOf(days.startsOn), endsOn: midnightOf(days.endsOn) };
    }),
  };
}

function rangeDaysOf(range: SeasonPeriodInterval): SeasonPeriodInterval {
  return { startsOn: dayOf(range.startsOn), endsOn: dayOf(range.endsOn) };
}

function daysOf(dates: SeasonDates): SeasonDates {
  return {
    startsOn: dayOf(dates.startsOn),
    endsOn: dayOf(dates.endsOn),
    nextStartsOn: dayOf(dates.nextStartsOn),
    nextEndsOn: dayOf(dates.nextEndsOn),
  };
}

function midnightOf(day: string | null): string | null {
  return day ? `${day.slice(0, 10)}T00:00:00.000Z` : null;
}

function dayOf(date: string | null): string | null {
  return date ? date.slice(0, 10) : null;
}
