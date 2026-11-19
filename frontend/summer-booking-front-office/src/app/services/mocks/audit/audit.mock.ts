import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { FilterField } from '../../../entities/enums/filter-field';
import {
  DEFAULT_PAGE_SIZE,
  Page,
  PageFilter,
  PageRequest,
} from '../../../entities/pagination/page';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { AuditEntryItem } from '../../../entities/settings/user-management/audit-entry-item';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
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
  entry(1, '01', '1234', 'Giulia Ferri', 'A1', SPOTS_CREATED, 1),
  entry(1, '02', '5678', 'Marco Bianchi', 'A2', PERIOD_UPDATED, 3),
  entry(1, '03', '9123', 'Sara Conti', 'A6', SPOT_UPDATED, 20),
  entry(1, '04', '4567', 'Luca Moretti', 'A12', SPOTS_CREATED, 30),
  entry(1, '05', '7890', 'Elena Gallo', 'B13', PERIOD_UPDATED, 52),
  entry(1, '06', '1254', 'Paolo Costa', 'C4', SPOT_UPDATED, 75),
  entry(1, '07', '5236', 'Chiara Fontana', 'D7', SPOTS_CREATED, 100),
  entry(1, '08', '8536', 'Davide Rinaldi', 'A7', PERIOD_UPDATED, 140),
  entry(1, '09', '0586', 'Anna Caruso', 'B11', PERIOD_UPDATED, 200),
  entry(1, '10', '1839', 'Matteo Lombardi', 'C9', SEASON_UPDATED, 400),
  entry(1, '11', '2741', 'Giulia Ferri', 'D2', SEASON_UPDATED, 900),
  entry(1, '12', '3365', 'Marco Bianchi', 'B5', SPOT_UPDATED, 2000),
  entry(2, '13', '7012', 'Laura Testa', 'A3', SPOTS_CREATED, 10),
];

const MAX_PAGE_SIZE = 100;
const SORT_FIELDS = [
  'bookingNumber',
  'actorName',
  'spotLabel',
  'actionCode',
  'occurredAt',
] as const;

/**
 * `POST /api/audit/entries-list`: one page of the log of the property of the token, the newest
 * first when no other order is asked. The search looks in the operator name, the booking number and
 * the spot; the period arrives in `filters` as `datetimeFrom` and `datetimeTo`. 401 without a valid
 * access token.
 */
export const entriesListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = DEMO_PROPERTY;
  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const from = filterValue(body.filters, FilterField.DatetimeFrom);
  const to = filterValue(body.filters, FilterField.DatetimeTo);
  const actionCodes = filterValues(body.filters, FilterField.ActionCode);
  const idUsers = filterValues(body.filters, FilterField.IdUser);
  const found = AUDIT_ENTRIES.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search ||
        [row.actorName, row.bookingNumber, row.spotLabel].some((text) =>
          text?.toLowerCase().includes(search),
        )) &&
      (!from || (row.occurredAt ?? '') >= from) &&
      (!to || (row.occurredAt ?? '') <= to) &&
      (!actionCodes.length || actionCodes.includes(row.actionCode ?? '')) &&
      (!idUsers.length || idUsers.includes(row.idUser)),
  ).map((row) => onlyFieldsOf(AuditEntryItem, row));
  const field = SORT_FIELDS.find((name) => name === body.sortField) ?? 'occurredAt';
  const sign = body.sortDirection === 'Ascending' && body.sortField ? 1 : -1;
  found.sort((first, second) => sign * compare(first[field], second[field]));
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

function entry(
  propertyId: number,
  number: string,
  bookingNumber: string,
  actorName: string,
  spotLabel: string,
  actionCode: string,
  hoursAgo: number,
): AuditEntryRow {
  return {
    propertyId,
    idAuditEntry: `9a4c2e1f-7b3d-4e5a-8c6b-0d1e2f3a4b${number}`,
    idUser: `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d${USER_NUMBERS[actorName]}`,
    bookingNumber,
    actorName,
    spotLabel,
    actionCode,
    occurredAt: new Date(Date.now() - hoursAgo * 60 * 60 * 1000).toISOString(),
  };
}

function compare(first: string | null, second: string | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? -1 : 1;
  }
  return first.localeCompare(second, 'it', { sensitivity: 'base' });
}

function filterValue(filters: unknown, field: FilterField): string | null {
  const filter = Array.isArray(filters)
    ? (filters as Partial<PageFilter>[]).find((item) => item.field === field)
    : undefined;
  const [value] = Array.isArray(filter?.value) ? filter.value : [];
  return typeof value === 'string' ? value : null;
}

function filterValues(filters: unknown, field: FilterField): string[] {
  const filter = Array.isArray(filters)
    ? (filters as Partial<PageFilter>[]).find((item) => item.field === field)
    : undefined;
  return Array.isArray(filter?.value)
    ? filter.value.filter((value): value is string => typeof value === 'string')
    : [];
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}
