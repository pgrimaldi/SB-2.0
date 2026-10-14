import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { DiscountType } from '../../../entities/enums/discount-type';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { VoucherItem } from '../../../entities/settings/vouchers/voucher-item';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { operationNotAllowed } from '../errors/problem.mock';
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
