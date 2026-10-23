import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import {
  DEFAULT_PAGE_SIZE,
  Page,
  PageFilter,
  PageRequest,
} from '../../../entities/pagination/page';
import { AuditEntryItem } from '../../../entities/settings/user-management/audit-entry-item';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { forbidden } from '../errors/problem.mock';
import { propertyOf } from '../properties/properties.mock';

type AuditEntryRow = AuditEntryItem & { propertyId: number };

const SPOT_ADDED = 'booking.spot_added';
const REPEATED = 'booking.repeated';
const MOVED = 'booking.moved';
const DATES_CHANGED = 'booking.dates_changed';

const AUDIT_ENTRIES: AuditEntryRow[] = [
  entry(1, '01', '1234', '123', 'Giulia Ferri', 'A1', SPOT_ADDED, 1),
  entry(1, '02', '5678', '321', 'Marco Bianchi', 'A2', REPEATED, 3),
  entry(1, '03', '9123', '456', 'Sara Conti', 'A6', MOVED, 20),
  entry(1, '04', '4567', '654', 'Luca Moretti', 'A12', SPOT_ADDED, 30),
  entry(1, '05', '7890', '678', 'Elena Gallo', 'B13', REPEATED, 52),
  entry(1, '06', '1254', '876', 'Paolo Costa', 'C4', MOVED, 75),
  entry(1, '07', '5236', '789', 'Chiara Fontana', 'D7', SPOT_ADDED, 100),
  entry(1, '08', '8536', '987', 'Davide Rinaldi', 'A7', REPEATED, 140),
  entry(1, '09', '0586', '890', 'Anna Caruso', 'B11', REPEATED, 200),
  entry(1, '10', '1839', '098', 'Matteo Lombardi', 'C9', DATES_CHANGED, 400),
  entry(1, '11', '2741', '123', 'Giulia Ferri', 'D2', DATES_CHANGED, 900),
  entry(1, '12', '3365', '321', 'Marco Bianchi', 'B5', MOVED, 2000),
  entry(2, '13', '7012', '555', 'Laura Testa', 'A3', SPOT_ADDED, 10),
];

const MAX_PAGE_SIZE = 100;
const SORT_FIELDS = [
  'bookingNumber',
  'userNumber',
  'actorName',
  'spotLabel',
  'actionCode',
  'occurredAt',
] as const;

/**
 * `POST /api/audit/entries-list`: one page of the log of the requested property, the newest first
 * when no other order is asked. The search looks in the operator name, the booking number and the
 * spot; the period arrives in `filters` as `datetimeFrom` and `datetimeTo`. 401 without a valid access
 * token; 403 for a missing or unknown property.
 */
export const entriesListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const from = filterValue(body.filters, 'datetimeFrom');
  const to = filterValue(body.filters, 'datetimeTo');
  const found = AUDIT_ENTRIES.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search ||
        [row.actorName, row.bookingNumber, row.spotLabel].some((text) =>
          text?.toLowerCase().includes(search),
        )) &&
      (!from || (row.occurredAt ?? '') >= from) &&
      (!to || (row.occurredAt ?? '') <= to),
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

function entry(
  propertyId: number,
  number: string,
  bookingNumber: string,
  userNumber: string,
  actorName: string,
  spotLabel: string,
  actionCode: string,
  hoursAgo: number,
): AuditEntryRow {
  return {
    propertyId,
    idAuditEntry: `9a4c2e1f-7b3d-4e5a-8c6b-0d1e2f3a4b${number}`,
    bookingNumber,
    userNumber,
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

function filterValue(filters: unknown, field: string): string | null {
  const filter = Array.isArray(filters)
    ? (filters as Partial<PageFilter>[]).find((item) => item.field === field)
    : undefined;
  const [value] = Array.isArray(filter?.value) ? filter.value : [];
  return typeof value === 'string' ? value : null;
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}
