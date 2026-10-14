import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { DiscountType } from '../../../entities/enums/discount-type';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { DeleteVoucherItemsRequest } from '../../../entities/settings/vouchers/delete-voucher-items-request';
import { DuplicateVoucherItemsRequest } from '../../../entities/settings/vouchers/duplicate-voucher-items-request';
import { VoucherItem } from '../../../entities/settings/vouchers/voucher-item';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { operationNotAllowed, resourceNotFound } from '../errors/problem.mock';
import { propertyOf } from '../properties/properties.mock';

type VoucherRow = VoucherItem & { propertyId: number };

const VOUCHERS: VoucherRow[] = [
  {
    propertyId: 1,
    idVoucher: '6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01',
    voucherName: 'Giugno',
    validFrom: '2026-06-01T00:00:00.000Z',
    validTo: '2026-06-30T00:00:00.000Z',
    discountType: DiscountType.Percentage,
    discountValue: 10,
  },
  {
    propertyId: 1,
    idVoucher: '6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02',
    voucherName: 'Ferragosto',
    validFrom: '2026-08-12T00:00:00.000Z',
    validTo: '2026-08-16T00:00:00.000Z',
    discountType: DiscountType.Fixed,
    discountValue: 5,
  },
];

const MAX_PAGE_SIZE = 100;
const SORT_FIELDS = [
  'voucherName',
  'validFrom',
  'validTo',
  'discountType',
  'discountValue',
] as const;

/**
 * `POST /api/vouchers/voucher-list`: one page of the discounts and vouchers of the requested
 * property. 401 without a valid access token; 403 for a missing or unknown property.
 */
export const voucherListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
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
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = VOUCHERS.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search || row.voucherName?.toLowerCase().includes(search)),
  ).map(({ propertyId: _propertyId, ...item }): VoucherItem => item);
  const field = SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }
  const answer: Page<VoucherItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/**
 * `POST /api/vouchers/delete-voucher-item` with `DeleteVoucherItemsRequest`: all or nothing. If one id
 * is not a voucher of the property nothing is deleted (404 `resource.not_found`). Repeated ids count
 * once. 204 without a body; 401 without a valid access token; 403 for an unknown property.
 */
export const deleteVoucherItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DeleteVoucherItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return operationNotAllowed(request);
  }
  const vouchers = vouchersOf(property.id, body.idVouchers);
  if (!vouchers) {
    return resourceNotFound(request);
  }
  for (const voucher of vouchers) {
    VOUCHERS.splice(VOUCHERS.indexOf(voucher), 1);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/vouchers/duplicate-voucher-item` with `DuplicateVoucherItemsRequest`: all or nothing,
 * like the delete. Each copy keeps dates, type and value; the name is chosen by the backend: the mock
 * invents "Giugno (copia)", then "Giugno (copia 2)" and so on.
 */
export const duplicateVoucherItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DuplicateVoucherItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return operationNotAllowed(request);
  }
  const vouchers = vouchersOf(property.id, body.idVouchers);
  if (!vouchers) {
    return resourceNotFound(request);
  }
  for (const voucher of vouchers) {
    VOUCHERS.push({ ...voucher, idVoucher: crypto.randomUUID(), voucherName: copyName(voucher) });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * The vouchers of the property for `idVouchers` (repeated ids once); null when one id is not one of
 * them, or there are none: the whole request is cancelled.
 */
function vouchersOf(propertyId: number, idVouchers: unknown): VoucherRow[] | null {
  const ids = Array.isArray(idVouchers) ? [...new Set(idVouchers)] : [];
  const vouchers = ids
    .map((id) => VOUCHERS.find((row) => row.idVoucher === id && row.propertyId === propertyId))
    .filter((voucher): voucher is VoucherRow => voucher !== undefined);
  return ids.length && vouchers.length === ids.length ? vouchers : null;
}

function copyName(voucher: VoucherRow): string {
  const taken = new Set(
    VOUCHERS.filter((row) => row.propertyId === voucher.propertyId).map((row) => row.voucherName),
  );
  let copy = `${voucher.voucherName} (copia)`;
  for (let number = 2; taken.has(copy); number++) {
    copy = `${voucher.voucherName} (copia ${number})`;
  }
  return copy;
}

/** Numbers by value, texts alphabetically; a missing value first ascending, last descending. */
function compare(first: string | number | null, second: string | number | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? -1 : 1;
  }
  return typeof first === 'number' && typeof second === 'number'
    ? first - second
    : String(first).localeCompare(String(second), 'it', { sensitivity: 'base' });
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}
