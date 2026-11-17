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
  private readonly endpoint = `${environment.apiBaseUrl}/seasons`;

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

  /**
   * The periods of the current season, or of the next one with `isNextSeason`, for the property of
   * the token.
   */
  readonly periodList = (isNextSeason: boolean): Observable<SeasonPeriodItem[]> =>
    this.httpClient.get<SeasonPeriodItem[]>(`${this.endpoint}/period-list`, {
      params: { isNextSeason },
    });

  /** A period of the current season, or of the next one with `isNextSeason`. Answers 204 with no body. */
  readonly addPeriod = (isNextSeason: boolean, request: SeasonPeriodAddRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/add-period`, {
      ...onlyFieldsOf(SeasonPeriodAddRequest, request),
      intervals: request.intervals.map((interval) => onlyFieldsOf(SeasonPeriodInterval, interval)),
      isNextSeason,
    });

  /** The period with all its date intervals. Answers 204 with no body. */
  readonly deletePeriod = (idPeriod: string): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-period`, { idPeriod });

  /** New name of a period. Answers 204 with no body. */
  readonly updatePeriod = (request: SeasonPeriodUpdateRequest): Observable<void> =>
    this.httpClient.post<void>(
      `${this.endpoint}/update-period`,
      onlyFieldsOf(SeasonPeriodUpdateRequest, request),
    );

  /** A new date interval of a period. Answers 204 with no body. */
  readonly addInterval = (idPeriod: string, interval: SeasonPeriodInterval): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/add-interval`, {
      ...onlyFieldsOf(SeasonPeriodInterval, interval),
      idPeriod,
    });

  /** New days of a date interval. Answers 204 with no body. */
  readonly updateInterval = (interval: SeasonPeriodIntervalItem): Observable<void> =>
    this.httpClient.post<void>(
      `${this.endpoint}/update-interval`,
      onlyFieldsOf(SeasonPeriodIntervalItem, interval),
    );

  /** One or more date intervals, of any period of the property: all or none. Answers 204 with no body. */
  readonly deleteIntervals = (idIntervals: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-intervals`, { idIntervals });
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
