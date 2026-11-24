import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, from, of, switchMap } from 'rxjs';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { ExistingBookingItem } from '../../../entities/database/existing-booking-item';
import { MAX_BOOKING_IDS } from '../../../entities/database/move-to-trash-request';
import { FilterField } from '../../../entities/enums/filter-field';
import { ReservationKind } from '../../../entities/enums/reservation-kind';
import { ReservationStatus } from '../../../entities/enums/reservation-status';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import {
  DEFAULT_PAGE_SIZE,
  Page,
  PageFilter,
  PageRequest,
} from '../../../entities/pagination/page';
import { BulkResult } from '../../../entities/shared/bulk-result';
import { isAuthorized, isMockPassword, unauthorized } from '../auth/auth.mock';
import { problem } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';

type ReservationRow = Omit<ExistingBookingItem, 'isTrashed'> & { propertyId: number };

/**
 * Rows of `booking.reservations` with the customer and agreement names already joined: same
 * structure as the sample DB, invented values. `stay_from`/`stay_to` are days: the API sends them
 * as UTC midnight.
 */
const RESERVATIONS: ReservationRow[] = [
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a01',
    customerName: 'Gianni Riva',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-06-01T00:00:00.000Z',
    dateTo: '2026-06-30T00:00:00.000Z',
    agreementName: null,
    reservationStatus: ReservationStatus.Confirmed,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a02',
    customerName: 'Luca Verdi',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-06-15T00:00:00.000Z',
    dateTo: '2026-06-15T00:00:00.000Z',
    agreementName: 'Hotel Esempio',
    reservationStatus: ReservationStatus.Confirmed,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a03',
    customerName: 'Sara Neri',
    reservationKind: ReservationKind.Service,
    dateFrom: '2026-07-02T00:00:00.000Z',
    dateTo: '2026-07-02T00:00:00.000Z',
    agreementName: null,
    reservationStatus: ReservationStatus.Pending,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a04',
    customerName: 'Marco Gialli',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-07-05T00:00:00.000Z',
    dateTo: '2026-07-12T00:00:00.000Z',
    agreementName: 'Residence Prova',
    reservationStatus: ReservationStatus.Cancelled,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a05',
    customerName: 'Anna Blu',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-07-10T00:00:00.000Z',
    dateTo: '2026-07-10T00:00:00.000Z',
    agreementName: null,
    reservationStatus: ReservationStatus.Confirmed,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a06',
    customerName: 'Paolo Viola',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-07-14T00:00:00.000Z',
    dateTo: '2026-08-14T00:00:00.000Z',
    agreementName: 'Hotel Esempio',
    reservationStatus: ReservationStatus.Confirmed,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a07',
    customerName: 'Elena Rosa',
    reservationKind: ReservationKind.Service,
    dateFrom: '2026-07-20T00:00:00.000Z',
    dateTo: '2026-07-20T00:00:00.000Z',
    agreementName: null,
    reservationStatus: ReservationStatus.Confirmed,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a08',
    customerName: 'Davide Grigi',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-07-21T00:00:00.000Z',
    dateTo: '2026-07-27T00:00:00.000Z',
    agreementName: null,
    reservationStatus: ReservationStatus.Pending,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a09',
    customerName: 'Chiara Marroni',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-08-01T00:00:00.000Z',
    dateTo: '2026-08-31T00:00:00.000Z',
    agreementName: 'Residence Prova',
    reservationStatus: ReservationStatus.Confirmed,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a10',
    customerName: 'Fabio Azzurri',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-08-03T00:00:00.000Z',
    dateTo: '2026-08-03T00:00:00.000Z',
    agreementName: null,
    reservationStatus: ReservationStatus.Cancelled,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a11',
    customerName: 'Irene Celesti',
    reservationKind: ReservationKind.Service,
    dateFrom: '2026-08-08T00:00:00.000Z',
    dateTo: '2026-08-08T00:00:00.000Z',
    agreementName: null,
    reservationStatus: ReservationStatus.Confirmed,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a12',
    customerName: 'Giorgio Arancio',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-08-10T00:00:00.000Z',
    dateTo: '2026-08-16T00:00:00.000Z',
    agreementName: 'Hotel Esempio',
    reservationStatus: ReservationStatus.Confirmed,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a13',
    customerName: 'Laura Ocra',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-08-15T00:00:00.000Z',
    dateTo: '2026-08-15T00:00:00.000Z',
    agreementName: null,
    reservationStatus: ReservationStatus.Confirmed,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a14',
    customerName: 'Stefano Indaco',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-08-20T00:00:00.000Z',
    dateTo: '2026-09-05T00:00:00.000Z',
    agreementName: null,
    reservationStatus: ReservationStatus.Confirmed,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a15',
    customerName: 'Marta Corallo',
    reservationKind: ReservationKind.Service,
    dateFrom: '2026-09-01T00:00:00.000Z',
    dateTo: '2026-09-01T00:00:00.000Z',
    agreementName: null,
    reservationStatus: ReservationStatus.Pending,
  },
  {
    propertyId: 2,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a16',
    customerName: 'Pietro Ambra',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-07-01T00:00:00.000Z',
    dateTo: '2026-07-31T00:00:00.000Z',
    agreementName: null,
    reservationStatus: ReservationStatus.Confirmed,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a17',
    customerName: 'Roberto Lilla',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-06-05T00:00:00.000Z',
    dateTo: '2026-06-07T00:00:00.000Z',
    agreementName: null,
    reservationStatus: ReservationStatus.Cancelled,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a18',
    customerName: 'Silvia Senape',
    reservationKind: ReservationKind.Service,
    dateFrom: '2026-07-18T00:00:00.000Z',
    dateTo: '2026-07-18T00:00:00.000Z',
    agreementName: 'Hotel Esempio',
    reservationStatus: ReservationStatus.Cancelled,
  },
  {
    propertyId: 1,
    idBooking: '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a19',
    customerName: 'Tommaso Ruggine',
    reservationKind: ReservationKind.Spot,
    dateFrom: '2026-08-22T00:00:00.000Z',
    dateTo: '2026-08-29T00:00:00.000Z',
    agreementName: null,
    reservationStatus: ReservationStatus.Cancelled,
  },
];

/** Hotels with an agreement with the property: invented names and ids. */
const AFFILIATED_HOTELS: { propertyId: number; id: string; value: string }[] = [
  { propertyId: 1, id: '5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a41', value: 'Hotel Miramare' },
  { propertyId: 1, id: '5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a42', value: 'Hotel 5 stelle' },
  { propertyId: 1, id: '5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a43', value: 'Hotel Sun' },
  { propertyId: 1, id: '5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a44', value: 'Beach Hotel' },
  { propertyId: 2, id: '5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a45', value: 'Hotel Esempio' },
];

/** Bookings in the trash: three from the start, then those moved there (until the page is reloaded). */
const trashedBookings = new Set<string>([
  '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a17',
  '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a18',
  '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a19',
]);

/** In the trash for more than 30 days: it cannot be restored. */
const EXPIRED_BOOKING = '0b6f1c2e-3a4d-4e5f-8a9b-1c2d3e4f5a19';

const MAX_PAGE_SIZE = 100;
const SORT_FIELDS = [
  'customerName',
  'reservationKind',
  'dateFrom',
  'dateTo',
  'agreementName',
  'reservationStatus',
] as const;

/**
 * `POST /api/booking/reservations/reservation-list`: one page of the bookings of the property of the token
 * (never of other properties) that are not in the trash. 401 without a valid access token.
 */
export const existingBookingListMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => bookingPageMock(request, false);

/** `POST /api/booking/trash/trash-list`: the same, for the bookings in the trash. */
export const trashItemListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> =>
  bookingPageMock(request, true);

function bookingPageMock(
  request: HttpRequest<unknown>,
  isTrash: boolean,
): Observable<HttpEvent<unknown>> {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = DEMO_PROPERTY;

  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const bookings = RESERVATIONS.filter(
    (row) => row.propertyId === property.id && trashedBookings.has(row.idBooking ?? '') === isTrash,
  ).map((row): ExistingBookingItem => ({
    idBooking: row.idBooking,
    customerName: row.customerName,
    reservationKind: row.reservationKind,
    dateFrom: row.dateFrom,
    dateTo: row.dateTo,
    agreementName: row.agreementName,
    reservationStatus: isTrash ? ReservationStatus.Cancelled : row.reservationStatus,
    isTrashed: isTrash,
  }));

  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const from = filterValue(body.filters, FilterField.DatetimeFrom);
  const to = filterValue(body.filters, FilterField.DatetimeTo);
  // A booking is in the period when some of its days are.
  const found = bookings.filter(
    (item) =>
      (!search || item.customerName?.trim().toLowerCase().includes(search)) &&
      (!from || (item.dateTo ?? '') >= from) &&
      (!to || (item.dateFrom ?? '') <= to),
  );
  const field = SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }

  const answer: Page<ExistingBookingItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
}

/**
 * `POST /api/catalog/agreement-partners/agreement-partner-combobox` without a body: the hotels of the property, id and
 * name. 401 without a valid access token.
 */
export const affiliatedHotelComboboxMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const property = DEMO_PROPERTY;
  const hotels: ComboboxItem[] = AFFILIATED_HOTELS.filter(
    (hotel) => hotel.propertyId === property.id,
  ).map(({ id, value }) => ({ id, value }));
  return of(new HttpResponse({ status: 200, url: request.url, body: hotels })).pipe(delay(150));
};

/**
 * `POST /api/booking/cleanup/move-to-trash` with `{ ids, password }`: checks the password of the user
 * (the one of the test account), then moves the bookings to the trash: they leave the list.
 */
export const moveToTrashMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const { password } = (request.body ?? {}) as { password?: unknown };
  return from(isMockPassword(password)).pipe(
    delay(600),
    switchMap((isRight) =>
      isRight
        ? bulkResult(request, false, ApiErrorCode.BookingReservationNotFound, (id) =>
            trashedBookings.add(id),
          )
        : problem(request, 422, ApiErrorCode.BookingPasswordInvalid),
    ),
  );
};

/**
 * `POST /api/booking/trash/empty` without a body: deletes for good every booking of the property
 * in the trash (until the page is reloaded) and says how many; 401 without a valid access token.
 */
export const emptyTrashMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const property = DEMO_PROPERTY;
  let purged = 0;
  for (let index = RESERVATIONS.length - 1; index >= 0; index--) {
    const row = RESERVATIONS[index];
    if (row.propertyId === property.id && trashedBookings.delete(row.idBooking ?? '')) {
      RESERVATIONS.splice(index, 1);
      purged++;
    }
  }
  return of(new HttpResponse({ status: 200, url: request.url, body: { purged } })).pipe(delay(600));
};

/** `POST /api/booking/trash/restore` with `{ ids }`: back to the bookings list. */
export const restoreBookingItemMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  isAuthorized(request)
    ? bulkResult(request, true, ApiErrorCode.BookingReservationNotInTrash, (id) =>
        trashedBookings.delete(id),
      )
    : unauthorized(request);

/** `POST /api/booking/trash/purge` with `{ ids }`: deletes the bookings in the trash for good. */
export const deleteSelectedItemMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  isAuthorized(request)
    ? bulkResult(request, true, ApiErrorCode.BookingReservationNotInTrash, (id) => {
        RESERVATIONS.splice(
          RESERVATIONS.findIndex((row) => row.idBooking === id),
          1,
        );
        trashedBookings.delete(id);
      })
    : unauthorized(request);

/** What can be done is done, the others come back with the reason, as the backend does. */
function bulkResult(
  request: HttpRequest<unknown>,
  isTrash: boolean,
  missing: ApiErrorCode,
  act: (id: string) => void,
): Observable<HttpEvent<unknown>> {
  const { ids } = (request.body ?? {}) as { ids?: unknown };
  if (!Array.isArray(ids) || !ids.length || ids.length > MAX_BOOKING_IDS) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors: [{ field: 'ids', code: ApiErrorCode.ValidationInvalid }],
    });
  }
  const body: BulkResult = { succeeded: [], failed: [] };
  for (const id of ids as string[]) {
    const isKnown = RESERVATIONS.some(
      (row) =>
        row.propertyId === DEMO_PROPERTY.id &&
        row.idBooking === id &&
        trashedBookings.has(id) === isTrash,
    );
    const code = !isKnown
      ? missing
      : request.url.endsWith('/restore') && id === EXPIRED_BOOKING
        ? ApiErrorCode.BookingRestoreWindowExpired
        : null;
    if (code) {
      body.failed.push({ id, code });
    } else {
      act(id);
      body.succeeded.push(id);
    }
  }
  return of(new HttpResponse({ status: 200, url: request.url, body })).pipe(delay(600));
}

/** Texts alphabetically, ignoring case; a missing value first ascending, last descending. */
function compare(first: string | null, second: string | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? -1 : 1;
  }
  return first.trim().localeCompare(second.trim(), 'it', { sensitivity: 'base' });
}

function filterValue(filters: unknown, field: FilterField): string | null {
  const filter = Array.isArray(filters)
    ? (filters as Partial<PageFilter>[]).find((item) => item.field === field)
    : undefined;
  const [value] = Array.isArray(filter?.value) ? filter.value : [];
  return typeof value === 'string' ? value : null;
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}
