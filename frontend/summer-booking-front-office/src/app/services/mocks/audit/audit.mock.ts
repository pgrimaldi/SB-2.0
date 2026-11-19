import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { TABLE_PAGE_SIZE_OPTIONS } from '../../../components/shared/ui/tables/table/table';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { FilterField } from '../../../entities/enums/filter-field';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { Page, PageFilter, PageRequest, SortDirection } from '../../../entities/pagination/page';
import { AuditEntryItem } from '../../../entities/settings/user-management/audit-entry-item';
import { MAX_AUDIT_SEARCH_LENGTH } from '../../../entities/settings/user-management/audit-log-filters-request';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { TIME_ZONE_HEADER } from '../../api/time-zone.interceptor';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';

type AuditEntryRow = AuditEntryItem & { propertyId: number; idUser: string };

const SPOTS_CREATED = 'catalog.map_spots_created';
const PERIOD_UPDATED = 'catalog.tariff_period_updated';
const SPOT_UPDATED = 'catalog.map_spot_updated';
const SEASON_UPDATED = 'catalog.season_updated';

/** The same ids as the users of identity.mock.ts. */
const USER_NUMBERS: Record<string, string> = {
  'Giulia Ferri': '01',
  'Marco Bianchi': '02',
  'Sara Conti': '03',
  'Luca Moretti': '04',
  'Elena Gallo': '05',
  'Paolo Costa': '06',
  'Chiara Fontana': '07',
  'Davide Rinaldi': '08',
  'Anna Caruso': '09',
  'Matteo Lombardi': '10',
  'Laura Testa': '13',
};

const AUDIT_ENTRIES: AuditEntryRow[] = [
  entry(1, 1201, '1234', 'Giulia Ferri', 'A1', SPOTS_CREATED, 1),
  entry(1, 1199, '5678', 'Marco Bianchi', 'A2', PERIOD_UPDATED, 3),
  entry(1, 1187, '9123', 'Sara Conti', 'A6', SPOT_UPDATED, 20),
  entry(1, 1180, '4567', 'Luca Moretti', 'A12', SPOTS_CREATED, 30),
  entry(1, 1176, '7890', 'Elena Gallo', 'B13', PERIOD_UPDATED, 52),
  entry(1, 1175, '1254', 'Paolo Costa', 'C4', SPOT_UPDATED, 52),
  entry(1, 1163, '5236', 'Chiara Fontana', 'D7', SPOTS_CREATED, 100),
  entry(1, 1158, '8536', 'Davide Rinaldi', 'A7', PERIOD_UPDATED, 140),
  entry(1, 1141, '0586', 'Anna Caruso', 'B11', PERIOD_UPDATED, 200),
  entry(1, 1120, '1839', 'Matteo Lombardi', 'C9', SEASON_UPDATED, 400),
  entry(1, 1087, '2741', 'Giulia Ferri', 'D2', SEASON_UPDATED, 900),
  entry(1, 1032, '3365', 'Marco Bianchi', 'B5', SPOT_UPDATED, 2000),
  entry(2, 1190, '7012', 'Laura Testa', 'A3', SPOTS_CREATED, 10),
];

const SORT_FIELDS = [
  'bookingNumber',
  'actorName',
  'spotLabel',
  'actionCode',
  'occurredAt',
] as const;
const SORT_DIRECTIONS: readonly SortDirection[] = ['Ascending', 'Descending'];
const FILTER_FIELDS: readonly string[] = [
  FilterField.DatetimeFrom,
  FilterField.DatetimeTo,
  FilterField.ActionCode,
  FilterField.IdUser,
];
/** The time zone of the backend when `X-Time-Zone` is missing or unknown (`ItalianTime`). */
const DEFAULT_TIME_ZONE = 'Europe/Rome';
/** `YYYY-MM-DD`: the first 10 characters of a filter of the period. */
const DAY_LENGTH = 10;

/**
 * `POST /api/audit/entries-list`: one page of the log of the property of the token. Without a sorted
 * column the newest first (`occurredAt`, then `idAuditEntry`, descending), as also for equal values
 * of a sorted column; `null` values go last ascending and first descending (PostgreSQL). The search
 * looks in the operator name, the booking number and the spot; the period arrives in `filters` as
 * `datetimeFrom` and `datetimeTo`, days read in the time zone of `X-Time-Zone`, both included. 400
 * for a search over 100 characters (`validation.max_length` on `search`), a last day before the first
 * (`validation.end_before_start` on `filters`) and the common rules of the tables
 * (`validation.invalid`); 401 without a valid access token.
 */
export const entriesListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const fromDay = filterValue(body.filters, FilterField.DatetimeFrom)?.slice(0, DAY_LENGTH) ?? null;
  const toDay = filterValue(body.filters, FilterField.DatetimeTo)?.slice(0, DAY_LENGTH) ?? null;
  const errors = entriesRequestErrors(body);
  if (fromDay && toDay && toDay < fromDay) {
    errors.push({ field: 'filters', code: ApiErrorCode.ValidationEndBeforeStart });
  }
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const { page = 1, pageSize = TABLE_PAGE_SIZE_OPTIONS[0] } = body;
  const timeZone = timeZoneOf(request.headers.get(TIME_ZONE_HEADER));
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const actionCodes = filterValues(body.filters, FilterField.ActionCode);
  const idUsers = filterValues(body.filters, FilterField.IdUser);
  const found = AUDIT_ENTRIES.filter((row) => {
    const day = row.occurredAt ? dayIn(row.occurredAt, timeZone) : null;
    return (
      row.propertyId === DEMO_PROPERTY.id &&
      (!search ||
        [row.actorName, row.bookingNumber, row.spotLabel].some((text) =>
          text?.toLowerCase().includes(search),
        )) &&
      (!fromDay || (day !== null && day >= fromDay)) &&
      (!toDay || (day !== null && day <= toDay)) &&
      (!actionCodes.length || actionCodes.includes(row.actionCode ?? '')) &&
      (!idUsers.length || idUsers.includes(row.idUser))
    );
  }).map((row) => onlyFieldsOf(AuditEntryItem, row));
  const field = SORT_FIELDS.find((name) => name === body.sortField);
  const sign = body.sortDirection === 'Descending' ? -1 : 1;
  found.sort(
    (first, second) =>
      (field ? sign * compare(first[field], second[field]) : 0) || newestFirst(first, second),
  );
  const answer: Page<AuditEntryItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/** The users that appear in the log of a property, once each: the users of the user-combobox mock. */
export function auditUsers(propertyId: number): ComboboxItem[] {
  const users = new Map<string, ComboboxItem>();
  for (const row of AUDIT_ENTRIES) {
    if (row.propertyId === propertyId) {
      users.set(row.idUser, { id: row.idUser, value: row.actorName ?? '' });
    }
  }
  return [...users.values()];
}

/** One `validation.invalid` per wrong field of the table request; `search` over 100 characters. */
function entriesRequestErrors(body: Partial<PageRequest>): MockFieldError[] {
  const filters: unknown[] = Array.isArray(body.filters) ? body.filters : [];
  const filterFields = filters.map((filter) => (filter as Partial<PageFilter>).field);
  const wrong = [
    { field: 'page', isWrong: !Number.isInteger(body.page) || (body.page ?? 0) < 1 },
    { field: 'pageSize', isWrong: !TABLE_PAGE_SIZE_OPTIONS.includes(body.pageSize ?? 0) },
    {
      field: 'sortField',
      isWrong: !!body.sortField && !SORT_FIELDS.some((name) => name === body.sortField),
    },
    {
      field: 'sortDirection',
      isWrong: !SORT_DIRECTIONS.some((direction) => direction === body.sortDirection),
    },
    {
      field: 'filters',
      isWrong:
        !Array.isArray(body.filters) ||
        filterFields.some((name) => !FILTER_FIELDS.includes(name ?? '')) ||
        new Set(filterFields).size !== filterFields.length,
    },
  ];
  const errors: MockFieldError[] = wrong
    .filter(({ isWrong }) => isWrong)
    .map(({ field }) => ({ field, code: ApiErrorCode.ValidationInvalid }));
  if (typeof body.search === 'string' && body.search.length > MAX_AUDIT_SEARCH_LENGTH) {
    errors.push({
      field: 'search',
      code: ApiErrorCode.ValidationMaxLength,
      args: { max: MAX_AUDIT_SEARCH_LENGTH, min: 0 },
    });
  }
  return errors;
}

/** The time zone of the header, the backend's one when it is missing or unknown. */
function timeZoneOf(header: string | null): string {
  try {
    return header
      ? new Intl.DateTimeFormat('en', { timeZone: header }).resolvedOptions().timeZone
      : DEFAULT_TIME_ZONE;
  } catch {
    return DEFAULT_TIME_ZONE;
  }
}

/** The day (`YYYY-MM-DD`) of an instant in a time zone. */
function dayIn(instant: string, timeZone: string): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(new Date(instant));
}

function newestFirst(first: AuditEntryItem, second: AuditEntryItem): number {
  return (
    compare(second.occurredAt, first.occurredAt) ||
    Number(second.idAuditEntry ?? 0) - Number(first.idAuditEntry ?? 0)
  );
}

function entry(
  propertyId: number,
  id: number,
  bookingNumber: string,
  actorName: string,
  spotLabel: string,
  actionCode: string,
  hoursAgo: number,
): AuditEntryRow {
  return {
    propertyId,
    idAuditEntry: String(id),
    idUser: `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d${USER_NUMBERS[actorName]}`,
    bookingNumber,
    actorName,
    spotLabel,
    actionCode,
    occurredAt: new Date(Date.now() - hoursAgo * 60 * 60 * 1000).toISOString(),
  };
}

/** `null` after every value, as PostgreSQL ascending: descending it comes first. */
function compare(first: string | null, second: string | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? 1 : -1;
  }
  return first.localeCompare(second, 'it', { sensitivity: 'base' });
}

function filterValue(filters: unknown, field: FilterField): string | null {
  const [value] = filterValues(filters, field);
  return value ?? null;
}

function filterValues(filters: unknown, field: FilterField): string[] {
  const filter = Array.isArray(filters)
    ? (filters as Partial<PageFilter>[]).find((item) => item.field === field)
    : undefined;
  return Array.isArray(filter?.value)
    ? filter.value.filter((value): value is string => typeof value === 'string')
    : [];
}
