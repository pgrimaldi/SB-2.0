import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, from, of, switchMap } from 'rxjs';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { ExistingBookingItem } from '../../../entities/database/existing-booking-item';
import { ReservationKind } from '../../../entities/enums/reservation-kind';
import { ReservationStatus } from '../../../entities/enums/reservation-status';
import {
  DEFAULT_PAGE_SIZE,
  Page,
  PageFilter,
  PageRequest,
} from '../../../entities/pagination/page';
import { isAuthorized, isMockPassword, unauthorized } from '../auth/auth.mock';
import { operationNotAllowed, problem, resourceNotFound } from '../errors/problem.mock';
import { propertyOf } from '../properties/properties.mock';

type ReservationRow = ExistingBookingItem & { propertyId: number };

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
    reservationStatus: ReservationStatus.NoShow,
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
    reservationStatus: ReservationStatus.NoShow,
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
];

/** Hotels with an agreement with the property: invented names and ids. */
const AFFILIATED_HOTELS: { propertyId: number; id: string; value: string }[] = [
  { propertyId: 1, id: '5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a41', value: 'Hotel Miramare' },
  { propertyId: 1, id: '5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a42', value: 'Hotel 5 stelle' },
  { propertyId: 1, id: '5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a43', value: 'Hotel Sun' },
  { propertyId: 1, id: '5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a44', value: 'Beach Hotel' },
  { propertyId: 2, id: '5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a45', value: 'Hotel Esempio' },
];

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
 * `POST /api/database/existing-booking-list`: one page of the bookings of the requested property
 * (never of other properties). 401 without a valid access token; 403 for a missing or unknown
 * property.
 */
export const existingBookingListMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return operationNotAllowed(request);
  }

  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const bookings = RESERVATIONS.filter((row) => row.propertyId === property.id).map(
    (row): ExistingBookingItem => ({
      idBooking: row.idBooking,
      customerName: row.customerName,
      reservationKind: row.reservationKind,
      dateFrom: row.dateFrom,
      dateTo: row.dateTo,
      agreementName: row.agreementName,
      reservationStatus: row.reservationStatus,
    }),
  );

  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const from = filterValue(body.filters, 'datetimeFrom');
  const to = filterValue(body.filters, 'datetimeTo');
  // A booking is in the period when some of its days are.
  const found = bookings.filter(
    (item) =>
      (!search || item.customerName?.toLowerCase().includes(search)) &&
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
};

/**
 * `POST /api/database/get-affiliated-hotel-combobox` with `{ idProperty }`: the hotels of the property,
 * id and name. 401 without a valid access token; 403 for a missing or unknown property.
 */
export const affiliatedHotelComboboxMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const property = propertyOf(((request.body ?? {}) as { idProperty?: unknown }).idProperty);
  if (!property) {
    return operationNotAllowed(request);
  }
  const hotels: ComboboxItem[] = AFFILIATED_HOTELS.filter(
    (hotel) => hotel.propertyId === property.id,
  ).map(({ id, value }) => ({ id, value }));
  return of(new HttpResponse({ status: 200, url: request.url, body: hotels })).pipe(delay(150));
};

/**
 * `POST /api/database/confirm-delete-booking` with `{ idProperty, idBookings, password }`: checks the
 * password of the user (the one of the test account) before the bookings are deleted; for now it
 * deletes nothing. 204 without a body; 401 without a valid access token; 403 for an unknown property;
 * 404 when a booking is not one of the property; 400 with an error on `password` when it is wrong.
 */
export const confirmDeleteBookingMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as {
    idProperty?: unknown;
    idBookings?: unknown;
    password?: unknown;
  };
  const property = propertyOf(body.idProperty);
  if (!property) {
    return operationNotAllowed(request);
  }
  const idBookings = Array.isArray(body.idBookings) ? body.idBookings : [];
  const known = RESERVATIONS.filter((row) => row.propertyId === property.id).map(
    (row) => row.idBooking,
  );
  if (!idBookings.length || idBookings.some((id) => !known.includes(id as string))) {
    return resourceNotFound(request);
  }
  return from(isMockPassword(body.password)).pipe(
    delay(600),
    switchMap((isRight) =>
      isRight
        ? of(new HttpResponse({ status: 204, url: request.url }))
        : problem(request, 400, 'validation.invalid_request', {
            title: 'Invalid request',
            errors: [{ field: 'password', code: 'auth.invalid_credentials' }],
          }),
    ),
  );
};

/** Texts alphabetically, ignoring case; a missing value first ascending, last descending. */
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
  return typeof filter?.value === 'string' ? filter.value : null;
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}
