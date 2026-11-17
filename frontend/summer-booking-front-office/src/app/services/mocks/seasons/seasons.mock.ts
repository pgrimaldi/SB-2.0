import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { SeasonDates } from '../../../entities/settings/seasons/season-dates';
import { SeasonPeriodAddRequest } from '../../../entities/settings/seasons/season-period-add-request';
import { SeasonPeriodInterval } from '../../../entities/settings/seasons/season-period-interval';
import { SeasonPeriodIntervalItem } from '../../../entities/settings/seasons/season-period-interval-item';
import { SeasonPeriodItem } from '../../../entities/settings/seasons/season-period-item';
import { SeasonPeriodUpdateRequest } from '../../../entities/settings/seasons/season-period-update-request';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem, resourceNotFound } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';

type SeasonDatesRow = SeasonDates & { propertyId: number };
type PeriodRow = SeasonPeriodItem & { propertyId: number; isNextSeason: boolean };
type AddPeriodBody = Partial<SeasonPeriodAddRequest> & {
  isNextSeason?: unknown;
};

const SEASON_DATES: SeasonDatesRow[] = [
  {
    propertyId: 1,
    startsOn: '2026-05-01T00:00:00.000Z',
    endsOn: '2026-09-30T00:00:00.000Z',
    nextStartsOn: '2027-05-01T00:00:00.000Z',
    nextEndsOn: '2027-09-30T00:00:00.000Z',
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
 * `GET /api/catalog/season`: first and last day of the current season and of the next one (the next
 * ones may be null) of the property of the token, as days (`2026-05-01`). 204 without a body while
 * the season is not set; 401 without a valid access token.
 */
export const seasonDatesMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = SEASON_DATES.find((dates) => dates.propertyId === DEMO_PROPERTY.id);
  if (!row) {
    return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(300));
  }
  const days: SeasonDates = {
    startsOn: dayOf(row.startsOn),
    endsOn: dayOf(row.endsOn),
    nextStartsOn: dayOf(row.nextStartsOn),
    nextEndsOn: dayOf(row.nextEndsOn),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: days })).pipe(delay(300));
};

/**
 * `PUT /api/catalog/season` with the four days (`2026-05-01`): the new season dates of the property of
 * the token (until the page is reloaded). 200 with the days as saved; 401 without a valid access token; 400
 * with one error per wrong day: each season needs both, no end before its start, and the next season
 * starts after the current one ends (`validation.out_of_range` on `nextStartsOn`).
 */
export const saveSeasonDatesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<SeasonDates>;
  const property = DEMO_PROPERTY;
  const errors: MockFieldError[] = [];
  const checkRange = (from: keyof SeasonDates, to: keyof SeasonDates): void => {
    const hasFrom = isDay(body[from]);
    const hasTo = isDay(body[to]);
    if (!hasFrom) {
      errors.push({ field: from, code: 'validation.required' });
    }
    if (!hasTo) {
      errors.push({ field: to, code: 'validation.required' });
    }
    if (hasFrom && hasTo && (body[to] as string) < (body[from] as string)) {
      errors.push({ field: to, code: 'validation.end_before_start' });
    }
  };
  checkRange('startsOn', 'endsOn');
  checkRange('nextStartsOn', 'nextEndsOn');
  if (
    isDay(body.endsOn) &&
    isDay(body.nextStartsOn) &&
    (body.nextStartsOn as string) <= (body.endsOn as string)
  ) {
    errors.push({ field: 'nextStartsOn', code: 'validation.out_of_range' });
  }
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const dates: SeasonDatesRow = {
    propertyId: property.id,
    startsOn: midnightOf(body.startsOn),
    endsOn: midnightOf(body.endsOn),
    nextStartsOn: midnightOf(body.nextStartsOn),
    nextEndsOn: midnightOf(body.nextEndsOn),
  };
  const index = SEASON_DATES.findIndex((row) => row.propertyId === property.id);
  if (index < 0) {
    SEASON_DATES.push(dates);
  } else {
    SEASON_DATES[index] = dates;
  }
  const days: SeasonDates = {
    startsOn: dayOf(dates.startsOn),
    endsOn: dayOf(dates.endsOn),
    nextStartsOn: dayOf(dates.nextStartsOn),
    nextEndsOn: dayOf(dates.nextEndsOn),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: days })).pipe(delay(600));
};

/**
 * `GET /api/seasons/period-list?isNextSeason=…`: the periods of the current season of the property of
 * the token (of the next one with `isNextSeason=true`), each with its date intervals. 401 without a
 * valid access token.
 */
export const seasonPeriodListMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const property = DEMO_PROPERTY;
  const isNextSeason = request.params.get('isNextSeason') === 'true';
  const periods = PERIODS.filter(
    (row) => row.propertyId === property.id && row.isNextSeason === isNextSeason,
  ).map((row) => structuredClone(onlyFieldsOf(SeasonPeriodItem, row)));
  return of(new HttpResponse({ status: 200, url: request.url, body: periods })).pipe(delay(300));
};

/**
 * `POST /api/seasons/add-period` with the name, the date intervals and `isNextSeason`: adds the
 * period to the current or to the next season (until the page is reloaded). 204 without a body; 401
 * without a valid access token; 400 with one error per wrong field.
 */
export const addSeasonPeriodMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as AddPeriodBody;
  const property = DEMO_PROPERTY;
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
 * `POST /api/seasons/delete-period` with `{ idPeriod }`: the period goes away with all its date
 * intervals (until the page is reloaded). 204 without a body; 401 without a valid access token; 404
 * `resource.not_found` for an id that is not one of its periods.
 */
export const deleteSeasonPeriodMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idPeriod?: string };
  const property = DEMO_PROPERTY;
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
 * `POST /api/seasons/update-period` with `idPeriod` and the new `periodName`: the period gets the
 * new name (until the page is reloaded). 204 without a body; 401 without a valid access token; 404
 * `resource.not_found` for an id that is not one of its periods; 400 for an empty name.
 */
export const updateSeasonPeriodMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<SeasonPeriodUpdateRequest>;
  const property = DEMO_PROPERTY;
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
 * `POST /api/seasons/add-interval` with the days and `idPeriod`: a new date interval of the period
 * (until the page is reloaded); one the period has already is not added. 204 without a body; 401
 * without a valid access token; 404 `resource.not_found` for an id that is not one of its periods;
 * 400 with one error per wrong day.
 */
export const addSeasonIntervalMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<SeasonPeriodInterval> & {
    idPeriod?: unknown;
  };
  const property = DEMO_PROPERTY;
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
 * `POST /api/seasons/update-interval` with the interval (`idInterval`, `dateFrom`, `dateTo`): new
 * days of the interval (until the page is reloaded). 204 without a body; 401 without a valid access
 * token; 404 `resource.not_found` for an id that is not an interval of its periods; 400 with one
 * error per wrong date.
 */
export const updateSeasonIntervalMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<SeasonPeriodIntervalItem>;
  const property = DEMO_PROPERTY;
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
 * `POST /api/seasons/delete-intervals` with `{ idIntervals }`: the date intervals go away from
 * their periods, all or none (until the page is reloaded); a period left without intervals stays.
 * 204 without a body; 401 without a valid access token; 400 for no ids; 404 `resource.not_found`
 * when one id is not an interval of its periods.
 */
export const deleteSeasonIntervalsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idIntervals?: string[] };
  const property = DEMO_PROPERTY;
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
  const dateFrom = (isNextSeason ? dates?.nextStartsOn : dates?.startsOn) ?? null;
  const dateTo = (isNextSeason ? dates?.nextEndsOn : dates?.endsOn) ?? null;
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

function dayOf(date: string | null): string | null {
  return date ? date.slice(0, 10) : null;
}

function midnightOf(day: string | null | undefined): string | null {
  return day ? `${day.slice(0, 10)}T00:00:00.000Z` : null;
}

function isDay(value: unknown): boolean {
  return typeof value === 'string' && !Number.isNaN(Date.parse(value));
}
