import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { DeleteIntervalsRequest } from '../../../entities/settings/seasons/delete-intervals-request';
import { DeletePeriodRequest } from '../../../entities/settings/seasons/delete-period-request';
import { SeasonDates } from '../../../entities/settings/seasons/season-dates';
import { SeasonPeriodAddRequest } from '../../../entities/settings/seasons/season-period-add-request';
import { SeasonPeriodInterval } from '../../../entities/settings/seasons/season-period-interval';
import { SeasonPeriodIntervalItem } from '../../../entities/settings/seasons/season-period-interval-item';
import { SeasonPeriodItem } from '../../../entities/settings/seasons/season-period-item';
import { SeasonPeriodUpdateRequest } from '../../../entities/settings/seasons/season-period-update-request';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, forbidden, problem, resourceNotFound } from '../errors/problem.mock';
import { propertyOf } from '../properties/properties.mock';

type SeasonDatesRow = SeasonDates & { propertyId: number };
type PeriodRow = SeasonPeriodItem & { propertyId: number; isNextSeason: boolean };
type AddPeriodBody = Partial<SeasonPeriodAddRequest> & {
  idProperty?: unknown;
  isNextSeason?: unknown;
};

const SEASON_DATES: SeasonDatesRow[] = [
  {
    propertyId: 1,
    currentSeasonDateFrom: '2026-05-01T00:00:00.000Z',
    currentSeasonDateTo: '2026-09-30T00:00:00.000Z',
    nextSeasonDateFrom: '2027-05-01T00:00:00.000Z',
    nextSeasonDateTo: '2027-09-30T00:00:00.000Z',
  },
];

const PERIODS: PeriodRow[] = [
  {
    propertyId: 1,
    isNextSeason: false,
    idPeriod: '3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e01',
    periodName: 'Bassa stagione',
    intervals: [
      {
        idInterval: '7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e01',
        dateFrom: '2026-05-01T00:00:00.000Z',
        dateTo: '2026-06-15T00:00:00.000Z',
      },
      {
        idInterval: '7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e02',
        dateFrom: '2026-09-01T00:00:00.000Z',
        dateTo: '2026-09-30T00:00:00.000Z',
      },
    ],
  },
  {
    propertyId: 1,
    isNextSeason: false,
    idPeriod: '3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e02',
    periodName: 'Media stagione',
    intervals: [
      {
        idInterval: '7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e03',
        dateFrom: '2026-06-16T00:00:00.000Z',
        dateTo: '2026-07-31T00:00:00.000Z',
      },
    ],
  },
  {
    propertyId: 1,
    isNextSeason: false,
    idPeriod: '3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e03',
    periodName: 'Alta stagione',
    intervals: [
      {
        idInterval: '7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e04',
        dateFrom: '2026-08-01T00:00:00.000Z',
        dateTo: '2026-08-31T00:00:00.000Z',
      },
    ],
  },
  {
    propertyId: 1,
    isNextSeason: true,
    idPeriod: '3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e04',
    periodName: 'Bassa stagione',
    intervals: [
      {
        idInterval: '7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e05',
        dateFrom: '2027-05-01T00:00:00.000Z',
        dateTo: '2027-06-30T00:00:00.000Z',
      },
      {
        idInterval: '7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e06',
        dateFrom: '2027-09-01T00:00:00.000Z',
        dateTo: '2027-09-30T00:00:00.000Z',
      },
    ],
  },
  {
    propertyId: 1,
    isNextSeason: true,
    idPeriod: '3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e05',
    periodName: 'Alta stagione',
    intervals: [
      {
        idInterval: '7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e07',
        dateFrom: '2027-07-01T00:00:00.000Z',
        dateTo: '2027-08-31T00:00:00.000Z',
      },
    ],
  },
];

/**
 * `GET /api/seasons/season-dates?idProperty=…`: first and last day of the current season and of the
 * next one (the next one may be null). 401 without a valid access token; 403 for a missing or
 * unknown property.
 */
export const seasonDatesMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const property = propertyOf(request.params.get('idProperty'));
  if (!property) {
    return forbidden(request);
  }
  const row = SEASON_DATES.find((dates) => dates.propertyId === property.id);
  const dates = row ? onlyFieldsOf(SeasonDates, row) : new SeasonDates();
  return of(new HttpResponse({ status: 200, url: request.url, body: dates })).pipe(delay(300));
};

/**
 * `POST /api/seasons/save-season-dates` with the four days and `idProperty`: the new season dates
 * (until the page is reloaded). 204 without a body; 401 without a valid access token; 403 for a
 * missing or unknown property; 400 with one error per wrong day: the current season needs both, the
 * next one both or none, and no end before its start.
 */
export const saveSeasonDatesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<SeasonDates> & { idProperty?: unknown };
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const errors: MockFieldError[] = [];
  const checkRange = (
    from: keyof SeasonDates,
    to: keyof SeasonDates,
    isRequired: boolean,
  ): void => {
    const hasFrom = isDay(body[from]);
    const hasTo = isDay(body[to]);
    if (!hasFrom && (isRequired || hasTo)) {
      errors.push({ field: from, code: 'validation.required' });
    }
    if (!hasTo && (isRequired || hasFrom)) {
      errors.push({ field: to, code: 'validation.required' });
    }
    if (hasFrom && hasTo && (body[to] as string) < (body[from] as string)) {
      errors.push({ field: to, code: 'validation.end_before_start' });
    }
  };
  checkRange('currentSeasonDateFrom', 'currentSeasonDateTo', true);
  checkRange('nextSeasonDateFrom', 'nextSeasonDateTo', false);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const dates: SeasonDatesRow = {
    propertyId: property.id,
    currentSeasonDateFrom: body.currentSeasonDateFrom ?? null,
    currentSeasonDateTo: body.currentSeasonDateTo ?? null,
    nextSeasonDateFrom: body.nextSeasonDateFrom ?? null,
    nextSeasonDateTo: body.nextSeasonDateTo ?? null,
  };
  const index = SEASON_DATES.findIndex((row) => row.propertyId === property.id);
  if (index < 0) {
    SEASON_DATES.push(dates);
  } else {
    SEASON_DATES[index] = dates;
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `GET /api/seasons/period-list?idProperty=…&isNextSeason=…`: the periods of the current season of
 * the property (of the next one with `isNextSeason=true`), each with its date intervals. 401 without
 * a valid access token; 403 for a missing or unknown property.
 */
export const seasonPeriodListMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const property = propertyOf(request.params.get('idProperty'));
  if (!property) {
    return forbidden(request);
  }
  const isNextSeason = request.params.get('isNextSeason') === 'true';
  const periods = PERIODS.filter(
    (row) => row.propertyId === property.id && row.isNextSeason === isNextSeason,
  ).map((row) => structuredClone(onlyFieldsOf(SeasonPeriodItem, row)));
  return of(new HttpResponse({ status: 200, url: request.url, body: periods })).pipe(delay(300));
};

/**
 * `POST /api/seasons/add-period` with the name, the date intervals, `idProperty` and `isNextSeason`:
 * adds the period to the current or to the next season (until the page is reloaded). 204 without a body; 401 without a valid access token; 403 for a
 * missing or unknown property; 400 with one error per wrong field.
 */
export const addSeasonPeriodMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as AddPeriodBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const errors = periodErrors(body);
  const season = seasonOf(property.id, body.isNextSeason === true);
  const intervals = Array.isArray(body.intervals) ? body.intervals : [];
  if (!errors.length && season && !intervals.every((interval) => isInside(interval, season))) {
    errors.push({ field: 'intervals', code: 'validation.out_of_range' });
  }
  const taken = intervalsOf(property.id, body.isNextSeason === true);
  if (!errors.length && hasOverlap(intervals, taken)) {
    errors.push({ field: 'intervals', code: 'validation.invalid' });
  }
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  PERIODS.push({
    propertyId: property.id,
    isNextSeason: body.isNextSeason === true,
    idPeriod: crypto.randomUUID(),
    periodName: (body.periodName as string).trim(),
    intervals: (body.intervals as SeasonPeriodInterval[]).map((interval) => ({
      idInterval: crypto.randomUUID(),
      ...onlyFieldsOf(SeasonPeriodInterval, interval),
    })),
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/seasons/delete-period` with `DeletePeriodRequest`: the period goes away with all its date
 * intervals (until the page is reloaded). 204 without a body; 401 without a valid access token; 403
 * for a missing or unknown property; 404 `resource.not_found` for an id that is not one of its periods.
 */
export const deleteSeasonPeriodMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DeletePeriodRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const period = PERIODS.find(
    (row) => row.idPeriod === body.idPeriod && row.propertyId === property.id,
  );
  if (!period) {
    return resourceNotFound(request);
  }
  PERIODS.splice(PERIODS.indexOf(period), 1);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/seasons/update-period` with `idPeriod`, the new `periodName` and `idProperty`: the period
 * gets the new name (until the page is reloaded). 204 without a body; 401 without a valid access
 * token; 403 for a missing or unknown property; 404 `resource.not_found` for an id that is not one of
 * its periods; 400 for an empty name.
 */
export const updateSeasonPeriodMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<SeasonPeriodUpdateRequest> & {
    idProperty?: unknown;
  };
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const period = PERIODS.find(
    (row) => row.idPeriod === body.idPeriod && row.propertyId === property.id,
  );
  if (!period) {
    return resourceNotFound(request);
  }
  if (typeof body.periodName !== 'string' || !body.periodName.trim()) {
    return problem(request, 400, 'validation.failed', {
      title: 'Invalid request',
      errors: [{ field: 'periodName', code: 'validation.required' }],
    });
  }
  period.periodName = body.periodName.trim();
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/seasons/add-interval` with the days, `idPeriod` and `idProperty`: a new date interval of
 * the period (until the page is reloaded); one the period has already is not added. 204 without a
 * body; 401 without a valid access token; 403 for a missing or unknown property; 404
 * `resource.not_found` for an id that is not one of its periods; 400 with one error per wrong day.
 */
export const addSeasonIntervalMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<SeasonPeriodInterval> & {
    idPeriod?: unknown;
    idProperty?: unknown;
  };
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const period = PERIODS.find(
    (row) => row.idPeriod === body.idPeriod && row.propertyId === property.id,
  );
  if (!period) {
    return resourceNotFound(request);
  }
  const season = seasonOf(property.id, period.isNextSeason);
  const errors: MockFieldError[] = [];
  if (!isDay(body.dateFrom)) {
    errors.push({ field: 'dateFrom', code: 'validation.required' });
  }
  if (!isDay(body.dateTo)) {
    errors.push({ field: 'dateTo', code: 'validation.required' });
  } else if (isDay(body.dateFrom) && (body.dateTo as string) < (body.dateFrom as string)) {
    errors.push({ field: 'dateTo', code: 'validation.end_before_start' });
  }
  if (season && isDay(body.dateFrom) && (body.dateFrom as string) < (season.dateFrom as string)) {
    errors.push({ field: 'dateFrom', code: 'validation.out_of_range' });
  }
  if (season && isDay(body.dateTo) && (body.dateTo as string) > (season.dateTo as string)) {
    errors.push({ field: 'dateTo', code: 'validation.out_of_range' });
  }
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  if (hasOverlap([body as SeasonPeriodInterval], intervalsOf(property.id, period.isNextSeason))) {
    return problem(request, 400, 'validation.failed', {
      title: 'Invalid request',
      errors: [{ field: 'dateTo', code: 'validation.invalid' }],
    });
  }
  period.intervals.push({
    idInterval: crypto.randomUUID(),
    dateFrom: body.dateFrom as string,
    dateTo: body.dateTo as string,
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/seasons/update-interval` with the interval (`idInterval`, `dateFrom`, `dateTo`) and
 * `idProperty`: new days of the interval (until the page is reloaded). 204 without a body; 401
 * without a valid access token; 403 for a missing or unknown property; 404 `resource.not_found` for
 * an id that is not an interval of its periods; 400 with one error per wrong date.
 */
export const updateSeasonIntervalMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<SeasonPeriodIntervalItem> & { idProperty?: unknown };
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const period = PERIODS.find(
    (row) =>
      row.propertyId === property.id &&
      row.intervals.some((interval) => interval.idInterval === body.idInterval),
  );
  const interval = period?.intervals.find((row) => row.idInterval === body.idInterval);
  if (!period || !interval) {
    return resourceNotFound(request);
  }
  const season = seasonOf(property.id, period.isNextSeason);
  const errors: MockFieldError[] = [];
  if (!isDay(body.dateFrom)) {
    errors.push({ field: 'dateFrom', code: 'validation.required' });
  }
  if (!isDay(body.dateTo)) {
    errors.push({ field: 'dateTo', code: 'validation.required' });
  } else if (isDay(body.dateFrom) && (body.dateTo as string) < (body.dateFrom as string)) {
    errors.push({ field: 'dateTo', code: 'validation.end_before_start' });
  }
  if (season && isDay(body.dateFrom) && (body.dateFrom as string) < (season.dateFrom as string)) {
    errors.push({ field: 'dateFrom', code: 'validation.out_of_range' });
  }
  if (season && isDay(body.dateTo) && (body.dateTo as string) > (season.dateTo as string)) {
    errors.push({ field: 'dateTo', code: 'validation.out_of_range' });
  }
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const others = intervalsOf(property.id, period.isNextSeason).filter(
    (other) => other.idInterval !== interval.idInterval,
  );
  if (hasOverlap([body as SeasonPeriodInterval], others)) {
    return problem(request, 400, 'validation.failed', {
      title: 'Invalid request',
      errors: [{ field: 'dateTo', code: 'validation.invalid' }],
    });
  }
  interval.dateFrom = body.dateFrom as string;
  interval.dateTo = body.dateTo as string;
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/seasons/delete-intervals` with `DeleteIntervalsRequest`: the date intervals go away from
 * their periods, all or none (until the page is reloaded); a period left without intervals stays.
 * 204 without a body; 401 without a valid access token; 403 for a missing or unknown property; 400
 * for no ids; 404 `resource.not_found` when one id is not an interval of its periods.
 */
export const deleteSeasonIntervalsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DeleteIntervalsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const idIntervals = Array.isArray(body.idIntervals) ? body.idIntervals : [];
  if (!idIntervals.length) {
    return problem(request, 400, 'validation.failed', {
      title: 'Invalid request',
      errors: [{ field: 'idIntervals', code: 'validation.required' }],
    });
  }
  const periods = PERIODS.filter((row) => row.propertyId === property.id);
  const isKnown = (idInterval: string): boolean =>
    periods.some((period) =>
      period.intervals.some((interval) => interval.idInterval === idInterval),
    );
  if (!idIntervals.every(isKnown)) {
    return resourceNotFound(request);
  }
  periods.forEach((period) => {
    period.intervals = period.intervals.filter(
      (interval) => !idIntervals.includes(interval.idInterval as string),
    );
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** As the backend checks it: a name and at least one interval, each with both days, the last not before the first. */
function periodErrors(body: AddPeriodBody): MockFieldError[] {
  const errors: MockFieldError[] = [];
  if (typeof body.periodName !== 'string' || !body.periodName.trim()) {
    errors.push({ field: 'periodName', code: 'validation.required' });
  }
  const intervals = Array.isArray(body.intervals) ? body.intervals : [];
  const isValid = (interval: SeasonPeriodInterval): boolean =>
    isDay(interval.dateFrom) &&
    isDay(interval.dateTo) &&
    (interval.dateTo as string) >= (interval.dateFrom as string);
  if (!intervals.length || !intervals.every(isValid)) {
    errors.push({ field: 'intervals', code: 'validation.required' });
  }
  return errors;
}

/** First and last day of the current or of the next season of the property, when both are set. */
function seasonOf(propertyId: number, isNextSeason: boolean): SeasonPeriodInterval | null {
  const dates = SEASON_DATES.find((row) => row.propertyId === propertyId);
  const dateFrom =
    (isNextSeason ? dates?.nextSeasonDateFrom : dates?.currentSeasonDateFrom) ?? null;
  const dateTo = (isNextSeason ? dates?.nextSeasonDateTo : dates?.currentSeasonDateTo) ?? null;
  return dateFrom && dateTo ? { dateFrom, dateTo } : null;
}

/** All the date intervals of the periods of the current or of the next season of the property. */
function intervalsOf(propertyId: number, isNextSeason: boolean): SeasonPeriodIntervalItem[] {
  return PERIODS.filter(
    (row) => row.propertyId === propertyId && row.isNextSeason === isNextSeason,
  ).flatMap((period) => period.intervals);
}

/** One of the new intervals shares a day with another new one or with one of the season. */
function hasOverlap(
  intervals: readonly SeasonPeriodInterval[],
  taken: readonly SeasonPeriodInterval[],
): boolean {
  const overlap = (first: SeasonPeriodInterval, second: SeasonPeriodInterval): boolean =>
    (first.dateFrom as string) <= (second.dateTo as string) &&
    (second.dateFrom as string) <= (first.dateTo as string);
  return intervals.some(
    (interval, index) =>
      taken.some((other) => overlap(interval, other)) ||
      intervals.some((other, otherIndex) => otherIndex !== index && overlap(interval, other)),
  );
}

function isInside(interval: SeasonPeriodInterval, season: SeasonPeriodInterval): boolean {
  return (
    (interval.dateFrom as string) >= (season.dateFrom as string) &&
    (interval.dateTo as string) <= (season.dateTo as string)
  );
}

function isDay(value: unknown): boolean {
  return typeof value === 'string' && !Number.isNaN(Date.parse(value));
}
