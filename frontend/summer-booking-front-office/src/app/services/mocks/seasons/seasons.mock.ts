import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { SeasonDates } from '../../../entities/settings/seasons/season-dates';
import { SeasonPeriodAddRequest } from '../../../entities/settings/seasons/season-period-add-request';
import { SeasonPeriodInterval } from '../../../entities/settings/seasons/season-period-interval';
import { SeasonPeriodIntervalItem } from '../../../entities/settings/seasons/season-period-interval-item';
import {
  MAX_SEASON_PERIOD_RANGES,
  SeasonPeriodItem,
} from '../../../entities/settings/seasons/season-period-item';
import { SeasonPeriodUpdateRequest } from '../../../entities/settings/seasons/season-period-update-request';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem, resourceNotFound } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';

type SeasonDatesRow = SeasonDates & { propertyId: number };
/** The days of its date ranges as the backend keeps them (`2026-05-01`). */
type PeriodRow = SeasonPeriodItem & { propertyId: number; isNextSeason: boolean };
type AddPeriodBody = Partial<SeasonPeriodAddRequest> & {
  isNextSeason?: unknown;
};

/** As the backend: the longest name and the most ranges deleted at once. */
const NAME_MAX_LENGTH = 80;
const DELETE_RANGES_MAX = 300;

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
    key: 0,
    name: 'Bassa stagione',
    ranges: [
      { startsOn: '2026-05-01', endsOn: '2026-06-15' },
      { startsOn: '2026-09-01', endsOn: '2026-09-30' },
    ],
  },
  {
    propertyId: 1,
    isNextSeason: false,
    key: 1,
    name: 'Media stagione',
    ranges: [{ startsOn: '2026-06-16', endsOn: '2026-07-31' }],
  },
  {
    propertyId: 1,
    isNextSeason: false,
    key: 2,
    name: 'Alta stagione',
    ranges: [{ startsOn: '2026-08-01', endsOn: '2026-08-31' }],
  },
  {
    propertyId: 1,
    isNextSeason: true,
    key: 0,
    name: 'Bassa stagione',
    ranges: [
      { startsOn: '2027-05-01', endsOn: '2027-06-30' },
      { startsOn: '2027-09-01', endsOn: '2027-09-30' },
    ],
  },
  {
    propertyId: 1,
    isNextSeason: true,
    key: 1,
    name: 'Alta stagione',
    ranges: [{ startsOn: '2027-07-01', endsOn: '2027-08-31' }],
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
 * `GET /api/catalog/tariff-periods` (current season) and `GET /api/catalog/next-season/tariff-periods`
 * (next season): the periods of the property of the token, each with its date ranges as days
 * (`2026-05-01`), in the order of their first day; `[]` while the season is not set. 401 without a
 * valid access token.
 */
export const seasonPeriodListMock = (
  request: HttpRequest<unknown>,
  isNextSeason: boolean,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const isSeasonSet = SEASON_DATES.some((row) => row.propertyId === DEMO_PROPERTY.id);
  const periods = isSeasonSet
    ? periodsOf(DEMO_PROPERTY.id, isNextSeason).map((row) =>
        structuredClone(onlyFieldsOf(SeasonPeriodItem, row)),
      )
    : [];
  return of(new HttpResponse({ status: 200, url: request.url, body: periods })).pipe(delay(300));
};

/**
 * `POST /api/seasons/add-period` with the name, the date ranges and `isNextSeason`: adds the period
 * to the current or to the next season (until the page is reloaded), with the next free key of that
 * season. 204 without a body; 401 without a valid access token; 400 with one error per wrong field;
 * 409 `pricing.period_name_taken` for the name of another period of the season.
 */
export const addSeasonPeriodMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as AddPeriodBody;
  const property = DEMO_PROPERTY;
  const isNextSeason = body.isNextSeason === true;
  const errors = addPeriodErrors(body);
  const season = seasonOf(property.id, isNextSeason);
  const ranges = Array.isArray(body.ranges) ? body.ranges.map(daysOf) : [];
  if (!errors.length && season && !ranges.every((range) => isInside(range, season))) {
    errors.push({ field: 'ranges', code: 'validation.out_of_range' });
  }
  if (!errors.length && hasOverlap(ranges, rangesOf(property.id, isNextSeason))) {
    errors.push({ field: 'ranges', code: 'validation.invalid' });
  }
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const name = (body.name as string).trim();
  const periods = periodsOf(property.id, isNextSeason);
  if (periods.some((row) => row.name?.toLowerCase() === name.toLowerCase())) {
    return problem(request, 409, 'pricing.period_name_taken', { title: 'Period name taken' });
  }
  const keys = periods.map(({ key }) => key ?? 0);
  PERIODS.push({
    propertyId: property.id,
    isNextSeason,
    key: Math.max(-1, ...keys) + 1,
    name,
    ranges: sorted(ranges),
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/seasons/delete-period` with `{ key, isNextSeason }`: the period of that season goes
 * away with all its date ranges (until the page is reloaded). 204 without a body; 401 without a
 * valid access token; 404 `resource.not_found` for a key that is not one of its periods.
 */
export const deleteSeasonPeriodMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { key?: unknown; isNextSeason?: unknown };
  const period = periodsOf(DEMO_PROPERTY.id, body.isNextSeason === true).find(
    ({ key }) => key === body.key,
  );
  if (!period) {
    return resourceNotFound(request);
  }
  PERIODS.splice(PERIODS.indexOf(period), 1);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `PUT /api/catalog/tariff-periods/{key}` and `PUT /api/catalog/next-season/tariff-periods/{key}`
 * with the name and all the date ranges of the period, as days: the period gets them (until the page
 * is reloaded), checked as the backend does. 200 with the period saved; 401 without a valid access
 * token; 400 with one error per wrong field; 422 `pricing.season_required` /
 * `pricing.next_season_required` while the season has no dates; 404 `pricing.period_not_found`; 409
 * `pricing.period_name_taken` for the name of another period of the season; 422
 * `pricing.period_outside_season` for a day outside the season.
 */
export const updateSeasonPeriodMock = (
  request: HttpRequest<unknown>,
  isNextSeason: boolean,
  key: number,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<SeasonPeriodUpdateRequest>;
  const errors = updatePeriodErrors(body);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const property = DEMO_PROPERTY;
  const season = seasonOf(property.id, isNextSeason);
  if (!season) {
    return seasonRequired(request, isNextSeason);
  }
  const periods = periodsOf(property.id, isNextSeason);
  const period = periods.find((row) => row.key === key);
  if (!period) {
    return problem(request, 404, 'pricing.period_not_found', { title: 'Period not found' });
  }
  const name = (body.name as string).trim();
  const isNameTaken = periods.some(
    (row) => row !== period && row.name?.toLowerCase() === name.toLowerCase(),
  );
  if (isNameTaken) {
    return problem(request, 409, 'pricing.period_name_taken', { title: 'Period name taken' });
  }
  const ranges = (body.ranges as SeasonPeriodInterval[]).map(daysOf);
  if (!ranges.every((range) => isInside(range, season))) {
    return problem(request, 422, 'pricing.period_outside_season', {
      title: 'Period outside the season',
      args: { seasonStartsOn: season.startsOn as string, seasonEndsOn: season.endsOn as string },
    });
  }
  period.name = name;
  period.ranges = sorted(ranges);
  const saved = structuredClone(onlyFieldsOf(SeasonPeriodItem, period));
  return of(new HttpResponse({ status: 200, url: request.url, body: saved })).pipe(delay(600));
};

/**
 * `POST /api/catalog/tariff-periods/bulk-delete-ranges` and
 * `POST /api/catalog/next-season/tariff-periods/bulk-delete-ranges` with `{ ranges }`, each with the
 * `key` of its period and its days: the date ranges go away from their periods, all or none (until
 * the page is reloaded). 204 without a body; 401 without a valid access token; 400 for no ranges,
 * more than 300 or one without its key or days; 422 `pricing.season_required` /
 * `pricing.next_season_required`; 404 `pricing.period_not_found` / `pricing.period_range_not_found`;
 * 422 `pricing.period_ranges_required` when a period would be left without ranges.
 */
export const deleteSeasonRangesMock = (
  request: HttpRequest<unknown>,
  isNextSeason: boolean,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { ranges?: unknown };
  const ranges: unknown[] = Array.isArray(body.ranges) ? body.ranges : [];
  if (!ranges.length || ranges.length > DELETE_RANGES_MAX || !ranges.every(isRangeOfPeriod)) {
    return problem(request, 400, 'validation.failed', {
      title: 'Invalid request',
      errors: [{ field: 'ranges', code: 'validation.required' }],
    });
  }
  const property = DEMO_PROPERTY;
  if (!seasonOf(property.id, isNextSeason)) {
    return seasonRequired(request, isNextSeason);
  }
  const periods = periodsOf(property.id, isNextSeason);
  const toDelete = (ranges as SeasonPeriodIntervalItem[]).map(daysOfRangeOfPeriod);
  if (!toDelete.every((item) => periods.some(({ key }) => key === item.key))) {
    return problem(request, 404, 'pricing.period_not_found', { title: 'Period not found' });
  }
  const isListed = (key: number | null, range: SeasonPeriodInterval): boolean =>
    toDelete.some(
      (item) =>
        item.key === key && item.startsOn === range.startsOn && item.endsOn === range.endsOn,
    );
  const isKnown = toDelete.every((item) =>
    periods.some(
      (period) =>
        period.key === item.key &&
        period.ranges.some(
          ({ startsOn, endsOn }) => startsOn === item.startsOn && endsOn === item.endsOn,
        ),
    ),
  );
  if (!isKnown) {
    return problem(request, 404, 'pricing.period_range_not_found', { title: 'Range not found' });
  }
  if (periods.some((period) => period.ranges.every((range) => isListed(period.key, range)))) {
    return problem(request, 422, 'pricing.period_ranges_required', {
      title: 'A period needs at least one range',
    });
  }
  periods.forEach((period) => {
    period.ranges = period.ranges.filter((range) => !isListed(period.key, range));
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** As our route checks it: a name and 1 to 20 ranges, each with both days, the last not before the first. */
function addPeriodErrors(body: AddPeriodBody): MockFieldError[] {
  const errors: MockFieldError[] = [];
  if (typeof body.name !== 'string' || !body.name.trim()) {
    errors.push({ field: 'name', code: 'validation.required' });
  }
  const ranges = Array.isArray(body.ranges) ? body.ranges : [];
  if (!ranges.length || !ranges.every(isValidRange)) {
    errors.push({ field: 'ranges', code: 'validation.required' });
  } else if (ranges.length > MAX_SEASON_PERIOD_RANGES) {
    errors.push({ field: 'ranges', code: 'validation.invalid' });
  }
  return errors;
}

/** As the backend's validator: a name up to 80 characters, 1 to 20 ranges, none ending before it starts, none overlapping. */
function updatePeriodErrors(body: Partial<SeasonPeriodUpdateRequest>): MockFieldError[] {
  const errors: MockFieldError[] = [];
  if (typeof body.name !== 'string' || !body.name.trim()) {
    errors.push({ field: 'name', code: 'validation.required' });
  } else if (body.name.length > NAME_MAX_LENGTH) {
    errors.push({ field: 'name', code: 'validation.max_length', args: { max: NAME_MAX_LENGTH } });
  }
  const ranges: SeasonPeriodInterval[] = Array.isArray(body.ranges) ? body.ranges : [];
  if (!ranges.length) {
    errors.push({ field: 'ranges', code: 'validation.required' });
  } else if (ranges.length > MAX_SEASON_PERIOD_RANGES) {
    errors.push({ field: 'ranges', code: 'validation.invalid' });
  }
  ranges.forEach((range, index) => {
    if (!isDay(range?.startsOn) || !isDay(range?.endsOn)) {
      errors.push({ field: `ranges[${index}]`, code: 'validation.required' });
    } else if (!isValidRange(range)) {
      errors.push({ field: `ranges[${index}]`, code: 'validation.end_before_start' });
    }
  });
  if (!errors.length && hasOverlap(ranges.map(daysOf), [])) {
    errors.push({ field: 'ranges', code: 'validation.invalid' });
  }
  return errors;
}

function seasonRequired(request: HttpRequest<unknown>, isNextSeason: boolean): Observable<never> {
  return isNextSeason
    ? problem(request, 422, 'pricing.next_season_required', { title: 'Next season required' })
    : problem(request, 422, 'pricing.season_required', { title: 'Season required' });
}

/** The periods of the current or of the next season of the property, in the order of their first day. */
function periodsOf(propertyId: number, isNextSeason: boolean): PeriodRow[] {
  return PERIODS.filter(
    (row) => row.propertyId === propertyId && row.isNextSeason === isNextSeason,
  ).sort((first, second) =>
    (first.ranges[0]?.startsOn ?? '').localeCompare(second.ranges[0]?.startsOn ?? ''),
  );
}

/** First and last day of the current or of the next season of the property, when both are set. */
function seasonOf(propertyId: number, isNextSeason: boolean): SeasonPeriodInterval | null {
  const dates = SEASON_DATES.find((row) => row.propertyId === propertyId);
  const startsOn = dayOf((isNextSeason ? dates?.nextStartsOn : dates?.startsOn) ?? null);
  const endsOn = dayOf((isNextSeason ? dates?.nextEndsOn : dates?.endsOn) ?? null);
  return startsOn && endsOn ? { startsOn, endsOn } : null;
}

/** All the date ranges of the periods of the current or of the next season of the property. */
function rangesOf(propertyId: number, isNextSeason: boolean): SeasonPeriodInterval[] {
  return periodsOf(propertyId, isNextSeason).flatMap((period) => period.ranges);
}

/** One of the new ranges shares a day with another new one or with one of `taken`. */
function hasOverlap(
  ranges: readonly SeasonPeriodInterval[],
  taken: readonly SeasonPeriodInterval[],
): boolean {
  const overlap = (first: SeasonPeriodInterval, second: SeasonPeriodInterval): boolean =>
    (first.startsOn as string) <= (second.endsOn as string) &&
    (second.startsOn as string) <= (first.endsOn as string);
  return ranges.some(
    (range, index) =>
      taken.some((other) => overlap(range, other)) ||
      ranges.some((other, otherIndex) => otherIndex !== index && overlap(range, other)),
  );
}

function isInside(range: SeasonPeriodInterval, season: SeasonPeriodInterval): boolean {
  return (
    (range.startsOn as string) >= (season.startsOn as string) &&
    (range.endsOn as string) <= (season.endsOn as string)
  );
}

function isValidRange(range: SeasonPeriodInterval): boolean {
  return (
    isDay(range?.startsOn) &&
    isDay(range?.endsOn) &&
    (range.endsOn as string).slice(0, 10) >= (range.startsOn as string).slice(0, 10)
  );
}

function isRangeOfPeriod(value: unknown): boolean {
  const range = value as Partial<SeasonPeriodIntervalItem> | null;
  return (
    typeof range === 'object' &&
    range !== null &&
    Number.isInteger(range.key) &&
    isDay(range.startsOn) &&
    isDay(range.endsOn)
  );
}

function sorted(ranges: SeasonPeriodInterval[]): SeasonPeriodInterval[] {
  return [...ranges].sort((first, second) =>
    (first.startsOn as string).localeCompare(second.startsOn as string),
  );
}

/** The days of a range, as the backend keeps them (`2026-05-01`). */
function daysOf(range: SeasonPeriodInterval): SeasonPeriodInterval {
  return { startsOn: dayOf(range.startsOn), endsOn: dayOf(range.endsOn) };
}

function daysOfRangeOfPeriod(range: SeasonPeriodIntervalItem): SeasonPeriodIntervalItem {
  return { key: range.key, ...daysOf(range) };
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
