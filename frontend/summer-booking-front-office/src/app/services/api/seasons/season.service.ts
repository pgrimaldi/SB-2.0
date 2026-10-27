import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { DeleteIntervalsRequest } from '../../../entities/settings/seasons/delete-intervals-request';
import { DeletePeriodRequest } from '../../../entities/settings/seasons/delete-period-request';
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

  readonly seasonDates = (idProperty: string): Observable<SeasonDates> =>
    this.httpClient.get<SeasonDates>(`${this.endpoint}/season-dates`, { params: { idProperty } });

  /** All four days, also the empty ones of the next season. Answers 204 with no body. */
  readonly saveSeasonDates = (idProperty: string, dates: SeasonDates): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/save-season-dates`, {
      ...onlyFieldsOf(SeasonDates, dates),
      idProperty,
    });

  /** The periods of the current season, or of the next one with `isNextSeason`. */
  readonly periodList = (
    idProperty: string,
    isNextSeason: boolean,
  ): Observable<SeasonPeriodItem[]> =>
    this.httpClient.get<SeasonPeriodItem[]>(`${this.endpoint}/period-list`, {
      params: { idProperty, isNextSeason },
    });

  /** A period of the current season, or of the next one with `isNextSeason`. Answers 204 with no body. */
  readonly addPeriod = (
    idProperty: string,
    isNextSeason: boolean,
    request: SeasonPeriodAddRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/add-period`, {
      ...onlyFieldsOf(SeasonPeriodAddRequest, request),
      intervals: request.intervals.map((interval) => onlyFieldsOf(SeasonPeriodInterval, interval)),
      idProperty,
      isNextSeason,
    });

  /** The period with all its date intervals. Answers 204 with no body. */
  readonly deletePeriod = (request: DeletePeriodRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-period`, request);

  /** New name of a period. Answers 204 with no body. */
  readonly updatePeriod = (
    idProperty: string,
    request: SeasonPeriodUpdateRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-period`, {
      ...onlyFieldsOf(SeasonPeriodUpdateRequest, request),
      idProperty,
    });

  /** A new date interval of a period; one it has already is not added. Answers 204 with no body. */
  readonly addInterval = (
    idProperty: string,
    idPeriod: string,
    interval: SeasonPeriodInterval,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/add-interval`, {
      ...onlyFieldsOf(SeasonPeriodInterval, interval),
      idPeriod,
      idProperty,
    });

  /** New days of a date interval. Answers 204 with no body. */
  readonly updateInterval = (
    idProperty: string,
    interval: SeasonPeriodIntervalItem,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-interval`, {
      ...onlyFieldsOf(SeasonPeriodIntervalItem, interval),
      idProperty,
    });

  /** One or more date intervals, of any period of the property: all or none. Answers 204 with no body. */
  readonly deleteIntervals = (request: DeleteIntervalsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-intervals`, request);
}
