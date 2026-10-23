import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { DiscountScope } from '../../../entities/enums/discount-scope';
import { DiscountType } from '../../../entities/enums/discount-type';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { DeleteVoucherItemsRequest } from '../../../entities/settings/vouchers/delete-voucher-items-request';
import { DuplicateVoucherItemsRequest } from '../../../entities/settings/vouchers/duplicate-voucher-items-request';
import { VoucherAddItemRequest } from '../../../entities/settings/vouchers/voucher-add-item-request';
import { VoucherItem } from '../../../entities/settings/vouchers/voucher-item';
import { VoucherSelectedItem } from '../../../entities/settings/vouchers/voucher-selected-item';
import { VoucherUpdateItemRequest } from '../../../entities/settings/vouchers/voucher-update-item-request';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, forbidden, problem, resourceNotFound } from '../errors/problem.mock';
import { propertyOf } from '../properties/properties.mock';

type VoucherRow = VoucherItem & VoucherSelectedItem & { propertyId: number };
type AddVoucherBody = Partial<VoucherAddItemRequest> & { idProperty?: unknown };
type UpdateVoucherBody = Partial<VoucherUpdateItemRequest> & { idProperty?: unknown };

const VOUCHERS: VoucherRow[] = [
  {
    propertyId: 1,
    idVoucher: '6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01',
    voucherName: 'Giugno',
    validFrom: '2026-06-01T00:00:00.000Z',
    validTo: '2026-06-30T00:00:00.000Z',
    validityPeriods: [
      {
        validFrom: '2026-06-01T00:00:00.000Z',
        validTo: '2026-06-30T00:00:00.000Z',
        daysOfWeek: [1, 2, 3, 4, 5, 6, 7],
        timeSlots: [],
      },
    ],
    discountType: DiscountType.Percentage,
    discountValue: 10,
    discountScope: DiscountScope.All,
    maxUseQuantity: 1,
    isEnabled: true,
  },
  {
    propertyId: 1,
    idVoucher: '6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02',
    voucherName: 'Ferragosto',
    validFrom: '2026-08-12T00:00:00.000Z',
    validTo: '2026-08-16T00:00:00.000Z',
    validityPeriods: [
      {
        validFrom: '2026-08-12T00:00:00.000Z',
        validTo: '2026-08-16T00:00:00.000Z',
        daysOfWeek: [1, 2, 3, 4, 5],
        timeSlots: [
          { from: '09:00', to: '13:00' },
          { from: '15:00', to: '19:00' },
        ],
      },
    ],
    discountType: DiscountType.Fixed,
    discountValue: 5,
    discountScope: DiscountScope.Services,
    maxUseQuantity: 50,
    isEnabled: false,
  },
];

const MAX_PAGE_SIZE = 100;
const TIME = /^([01]\d|2[0-3]):[0-5]\d$/;
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
    return forbidden(request);
  }
  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = VOUCHERS.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search || row.voucherName?.toLowerCase().includes(search)),
  ).map((row) => onlyFieldsOf(VoucherItem, row));
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
    return forbidden(request);
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
    return forbidden(request);
  }
  const vouchers = vouchersOf(property.id, body.idVouchers);
  if (!vouchers) {
    return resourceNotFound(request);
  }
  for (const voucher of vouchers) {
    VOUCHERS.push({
      ...structuredClone(voucher),
      idVoucher: crypto.randomUUID(),
      voucherName: copyName(voucher),
    });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/vouchers/add-voucher-item` with `VoucherAddItemRequest` and `idProperty`: a new voucher
 * with the name, the validity periods and the options; in the list it shows from the first day to
 * the last one. 204 without a body; 401 without a valid access token; 403 for an
 * unknown property; 400 with one error per wrong field (see `voucherErrors`).
 */
export const addVoucherItemMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as AddVoucherBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const errors = voucherErrors(body);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  VOUCHERS.push({ propertyId: property.id, idVoucher: crypto.randomUUID(), ...savedFields(body) });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/vouchers/update-voucher-item` with `VoucherUpdateItemRequest` and `idProperty`: the
 * voucher `idVoucher` takes the new values, after the same checks as the add (see `voucherErrors`).
 * 204 without a body; 401 without a valid access token; 403 for an unknown property; 404
 * `resource.not_found` for an id that is not one of its vouchers; 400 with one error per wrong field.
 */
export const updateVoucherItemMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as UpdateVoucherBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const voucher = VOUCHERS.find(
    (row) => row.idVoucher === body.idVoucher && row.propertyId === property.id,
  );
  if (!voucher) {
    return resourceNotFound(request);
  }
  const errors = voucherErrors(body);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  Object.assign(voucher, savedFields(body));
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `GET /api/vouchers/get-selected-item?idVoucher=…`: the voucher with every field of its form, for
 * the edit popup. 200 with `VoucherSelectedItem`; 401 without a valid access token; 404
 * `resource.not_found` for an unknown id.
 */
export const selectedVoucherItemMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const idVoucher = request.params.get('idVoucher');
  const voucher = VOUCHERS.find((row) => row.idVoucher === idVoucher);
  if (!voucher) {
    return resourceNotFound(request);
  }
  const answer = structuredClone(onlyFieldsOf(VoucherSelectedItem, voucher));
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(300));
};

/**
 * As the backend checks it: a name, at least one period, and in each period both days (the last not
 * before the first), at least one day of the week from 1 to 7 and time slots `HH:mm` (none is fine);
 * a known type and scope, a value from 1 (up to 100 for a percentage), at least 1 use, the switch.
 */
function voucherErrors(body: AddVoucherBody): MockFieldError[] {
  const errors: MockFieldError[] = [];
  if (typeof body.voucherName !== 'string' || !body.voucherName.trim()) {
    errors.push({ field: 'voucherName', code: 'validation.required' });
  }
  const periods = Array.isArray(body.validityPeriods) ? body.validityPeriods : [];
  if (!periods.length) {
    errors.push({ field: 'validityPeriods', code: 'validation.required' });
  }
  periods.forEach((period, index) => {
    const at = `validityPeriods[${index}]`;
    const isFromDay = isDay(period.validFrom);
    const isToDay = isDay(period.validTo);
    if (!isFromDay) {
      errors.push({ field: `${at}.validFrom`, code: 'validation.required' });
    }
    if (!isToDay) {
      errors.push({ field: `${at}.validTo`, code: 'validation.required' });
    }
    if (isFromDay && isToDay && (period.validTo as string) < (period.validFrom as string)) {
      errors.push({ field: `${at}.validTo`, code: 'validation.end_before_start' });
    }
    const days = Array.isArray(period.daysOfWeek) ? period.daysOfWeek : [];
    if (!days.length || days.some((day) => !Number.isInteger(day) || day < 1 || day > 7)) {
      errors.push({ field: `${at}.daysOfWeek`, code: 'validation.required' });
    }
    (Array.isArray(period.timeSlots) ? period.timeSlots : []).forEach((timeSlot, slotIndex) => {
      for (const side of ['from', 'to'] as const) {
        if (typeof timeSlot[side] !== 'string' || !TIME.test(timeSlot[side])) {
          errors.push({
            field: `${at}.timeSlots[${slotIndex}].${side}`,
            code: 'validation.invalid',
          });
        }
      }
    });
  });
  if (!Object.values(DiscountType).includes(body.discountType as DiscountType)) {
    errors.push({ field: 'discountType', code: 'validation.required' });
  }
  const value = body.discountValue;
  const highest = body.discountType === DiscountType.Percentage ? 100 : Infinity;
  if (typeof value !== 'number' || value < 1 || value > highest) {
    errors.push({ field: 'discountValue', code: 'validation.out_of_range' });
  }
  if (!Object.values(DiscountScope).includes(body.discountScope as DiscountScope)) {
    errors.push({ field: 'discountScope', code: 'validation.required' });
  }
  if (!Number.isInteger(body.maxUseQuantity) || (body.maxUseQuantity as number) < 1) {
    errors.push({ field: 'maxUseQuantity', code: 'validation.out_of_range' });
  }
  if (typeof body.isEnabled !== 'boolean') {
    errors.push({ field: 'isEnabled', code: 'validation.invalid' });
  }
  return errors;
}

/** The fields a valid body saves; in the list the voucher shows from its first day to its last. */
function savedFields(
  body: AddVoucherBody | UpdateVoucherBody,
): Omit<VoucherRow, 'propertyId' | 'idVoucher'> {
  const periods = body.validityPeriods ?? [];
  const days = periods.flatMap(({ validFrom, validTo }) => [
    validFrom as string,
    validTo as string,
  ]);
  days.sort();
  return {
    voucherName: (body.voucherName as string).trim(),
    validFrom: days[0] ?? null,
    validTo: days.at(-1) ?? null,
    validityPeriods: structuredClone(periods),
    discountType: body.discountType as DiscountType,
    discountValue: body.discountValue as number,
    discountScope: body.discountScope as DiscountScope,
    maxUseQuantity: body.maxUseQuantity as number,
    isEnabled: body.isEnabled as boolean,
  };
}

function isDay(value: unknown): boolean {
  return typeof value === 'string' && !Number.isNaN(Date.parse(value));
}

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
