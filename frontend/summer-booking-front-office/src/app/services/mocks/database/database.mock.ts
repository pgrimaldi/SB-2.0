import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ExistingBookingItem } from '../../../entities/database/existing-booking-item';
import { ExistingBookingListRequest } from '../../../entities/database/existing-booking-list-request';
import { ReservationKind } from '../../../entities/enums/reservation-kind';
import { ReservationStatus } from '../../../entities/enums/reservation-status';
import { DEFAULT_PAGE_SIZE, Page } from '../../../entities/pagination/page';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { operationNotAllowed } from '../errors/problem.mock';
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
    customerName: 'Giulia Bianchi',
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
  const body = (request.body ?? {}) as Partial<ExistingBookingListRequest>;
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

  // The server decides where to search: the mock looks in the customer and agreement names.
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = search
    ? bookings.filter((item) =>
        [item.customerName, item.agreementName].some((text) =>
          text?.toLowerCase().includes(search),
        ),
      )
    : bookings;
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

/** Texts alphabetically, ignoring case; a missing value first ascending, last descending. */
function compare(first: string | null, second: string | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? -1 : 1;
  }
  return first.localeCompare(second, 'it', { sensitivity: 'base' });
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}
