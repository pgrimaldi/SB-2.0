import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { DiscountScope } from '../../../entities/enums/discount-scope';
import { DiscountServiceCategory } from '../../../entities/enums/discount-service-category';
import { DiscountType } from '../../../entities/enums/discount-type';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import {
  MAX_FIXED_DISCOUNT,
  MAX_VOUCHER_CODE_LENGTH,
  MAX_VOUCHER_NAME_LENGTH,
  MAX_VOUCHER_PERIOD_QUANTITY,
  MAX_VOUCHER_TIME_SLOT_QUANTITY,
  MAX_VOUCHER_USE_QUANTITY,
  VoucherAddItemRequest,
} from '../../../entities/settings/vouchers/voucher-add-item-request';
import { VoucherItem } from '../../../entities/settings/vouchers/voucher-item';
import { VoucherSelectedItem } from '../../../entities/settings/vouchers/voucher-selected-item';
import { VoucherUpdateItemRequest } from '../../../entities/settings/vouchers/voucher-update-item-request';
import { Language } from '../../../entities/shared/language';
import { sameTextIgnoringCase } from '../../../components/shared/forms/text-comparison';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { missingSector } from '../catalog/service-checks.mock';
import { copyName, naturalOrder, tableRequestErrors } from '../catalog/service-tables.mock';
import { MockFieldError, problem } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';
import { savedWithVersion, versionConflict, versionOf } from '../versions/versions.mock';

type VoucherRow = Omit<VoucherSelectedItem, 'version'> & {
  propertyId: number;
  usedQuantity: number;
};
type AddVoucherBody = Partial<VoucherAddItemRequest>;
type UpdateVoucherBody = Partial<VoucherUpdateItemRequest>;

const VOUCHERS: VoucherRow[] = [
  {
    propertyId: 1,
    idVoucher: '6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01',
    voucherName: 'Giugno',
    voucherCode: 'GIUGNO10',
    isCodeRequired: true,
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
    serviceCategories: [],
    idSectors: [],
    maxUseQuantity: 1,
    isEnabled: true,
    usedQuantity: 0,
  },
  {
    propertyId: 1,
    idVoucher: '6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02',
    voucherName: 'Ferragosto',
    voucherCode: 'FERRAGOSTO5',
    isCodeRequired: true,
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
    discountScope: DiscountScope.Custom,
    serviceCategories: [DiscountServiceCategory.Bar, DiscountServiceCategory.Normal],
    idSectors: ['5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a10'],
    maxUseQuantity: 50,
    isEnabled: false,
    usedQuantity: 12,
  },
  {
    propertyId: 1,
    idVoucher: '6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f03',
    voucherName: 'Ultimo minuto',
    voucherCode: 'Ultimo minuto',
    isCodeRequired: false,
    validityPeriods: [
      {
        validFrom: '2026-07-01T00:00:00.000Z',
        validTo: '2026-07-31T00:00:00.000Z',
        daysOfWeek: [1, 2, 3, 4, 5, 6, 7],
        timeSlots: [],
      },
    ],
    discountType: DiscountType.Percentage,
    discountValue: 12.5,
    discountScope: DiscountScope.All,
    serviceCategories: [],
    idSectors: [],
    maxUseQuantity: null,
    isEnabled: true,
    usedQuantity: 3,
  },
  {
    propertyId: 1,
    idVoucher: '6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f04',
    voucherName: 'Clienti abituali',
    voucherCode: 'ABITUALI',
    isCodeRequired: true,
    validityPeriods: [],
    discountType: DiscountType.Fixed,
    discountValue: 15,
    discountScope: DiscountScope.WithoutCustom,
    serviceCategories: [DiscountServiceCategory.Additional],
    idSectors: [],
    maxUseQuantity: null,
    isEnabled: true,
    usedQuantity: 0,
  },
];

const TIME = /^([01]\d|2[0-3]):[0-5]\d$/;
const SORT_FIELDS = [
  'voucherName',
  'validFrom',
  'validTo',
  'discountType',
  'discountValue',
] as const;

/**
 * `POST /api/catalog/discounts/voucher-list`: one page of the discounts and vouchers of the property
 * of the token, by name unless sorted. 401 without a valid access token; 400 as `serviceListAnswer`.
 */
export const voucherListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const errors = tableRequestErrors(body, SORT_FIELDS);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const { page = 1, pageSize = DEFAULT_PAGE_SIZE } = body;
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const sign = body.sortDirection === 'Descending' ? -1 : 1;
  const field = SORT_FIELDS.find((name) => name === body.sortField) ?? 'voucherName';
  const found = vouchersOfProperty()
    .filter(
      ({ voucherName, voucherCode }) =>
        !search ||
        [voucherName, voucherCode].some((text) => text?.trim().toLowerCase().includes(search)),
    )
    .map(listItemOf)
    .sort(
      (first, second) =>
        compare(first[field], second[field], sign) ||
        naturalOrder(first.voucherName, second.voucherName),
    );
  const answer: Page<VoucherItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/** The name, or else the code; from the first day of the periods to the last one, none without periods. */
function listItemOf(voucher: VoucherRow): VoucherItem {
  const days = voucher.validityPeriods
    .flatMap(({ validFrom, validTo }) => [validFrom, validTo])
    .filter((day) => day !== null)
    .sort();
  return {
    idVoucher: voucher.idVoucher,
    voucherName: voucher.voucherName ?? voucher.voucherCode,
    validFrom: days[0] ?? null,
    validTo: days.at(-1) ?? null,
    discountType: voucher.discountType,
    discountValue: voucher.discountValue,
  };
}

/**
 * `POST /api/catalog/discounts/delete-voucher-item` with `{ idVouchers }`: all or nothing (see
 * `withVouchers`).
 */
export const deleteVoucherItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  withVouchers(request, (vouchers) => {
    for (const voucher of vouchers) {
      VOUCHERS.splice(VOUCHERS.indexOf(voucher), 1);
    }
  });

/**
 * `POST /api/catalog/discounts/duplicate-voucher-item` with `{ idVouchers }`: all or nothing (see
 * `withVouchers`). Each copy keeps every data, starts off and never used, and takes the first free
 * name and code ("Giugno copia", then "Giugno copia 2").
 */
export const duplicateVoucherItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  withVouchers(request, (vouchers) => {
    for (const voucher of vouchers) {
      const others = vouchersOfProperty();
      VOUCHERS.push({
        ...structuredClone(voucher),
        idVoucher: crypto.randomUUID(),
        voucherName:
          voucher.voucherName &&
          copyName(
            voucher.voucherName,
            Language.It,
            others.map(({ voucherName }) => voucherName),
            MAX_VOUCHER_NAME_LENGTH,
          ),
        voucherCode: copyName(
          voucher.voucherCode ?? '',
          Language.It,
          others.map(({ voucherCode }) => voucherCode),
          MAX_VOUCHER_CODE_LENGTH,
        ),
        isEnabled: false,
        usedQuantity: 0,
      });
    }
  });

/**
 * All or nothing: 404 `pricing.discount_not_found` when one of `idVouchers` is not a voucher of the
 * property, and nothing changes; 400 `validation.required` without ids; repeated ids count once. 204
 * without a body; 401 without a valid access token.
 */
function withVouchers(
  request: HttpRequest<unknown>,
  change: (vouchers: VoucherRow[]) => void,
): Observable<HttpEvent<unknown>> {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const { idVouchers } = (request.body ?? {}) as { idVouchers?: unknown };
  const ids = Array.isArray(idVouchers) ? [...new Set<unknown>(idVouchers)] : [];
  if (!ids.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors: [{ field: 'idVouchers', code: ApiErrorCode.ValidationRequired }],
    });
  }
  const vouchers = ids.flatMap((id) =>
    vouchersOfProperty().filter(({ idVoucher }) => idVoucher === id),
  );
  if (vouchers.length !== ids.length) {
    return problem(request, 404, ApiErrorCode.PricingDiscountNotFound);
  }
  change(vouchers);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
}

/** How many vouchers are limited to the sector. */
export function voucherQuantityOfSector(idSector: string | null): number {
  return vouchersOfProperty().filter(
    ({ idSectors }) => idSector !== null && idSectors.includes(idSector),
  ).length;
}

function vouchersOfProperty(): VoucherRow[] {
  return VOUCHERS.filter(({ propertyId }) => propertyId === DEMO_PROPERTY.id);
}

function voucherOf(idVoucher: unknown): VoucherRow | undefined {
  return vouchersOfProperty().find((voucher) => voucher.idVoucher === idVoucher);
}

/**
 * `POST /api/catalog/discounts/add-voucher-item` with `VoucherAddItemRequest`: a new voucher, with a
 * code to type. 204 without a body; 401 without a valid access token; 400 with one error per wrong
 * field (see `voucherErrors`); 409 `pricing.discount_code_taken`; 404 `catalog.sector_not_found`.
 */
export const addVoucherItemMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as AddVoucherBody;
  const errors = voucherErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors,
    });
  }
  const refused = codeTaken(request, body) ?? missingSector(request, body.idSectors);
  if (refused) {
    return refused;
  }
  VOUCHERS.push({
    propertyId: DEMO_PROPERTY.id,
    idVoucher: crypto.randomUUID(),
    usedQuantity: 0,
    ...savedFields(body),
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/catalog/discounts/update-voucher-item` with `VoucherUpdateItemRequest`: the voucher
 * `idVoucher` takes the new values. 204 without a body, with the new version in `ETag`; 401 without a
 * valid access token; 400 as the add; 404 `pricing.discount_not_found` for an id that is not one of
 * its vouchers; 409 `resource.version_conflict` when `If-Match` is not the version of the voucher, then
 * `pricing.discount_code_taken`; 422 `pricing.discount_max_uses_below_used`; 404
 * `catalog.sector_not_found` for a sector that is not of the property.
 */
export const updateVoucherItemMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as UpdateVoucherBody;
  const errors = voucherErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors,
    });
  }
  const voucher = voucherOf(body.idVoucher);
  if (!voucher) {
    return problem(request, 404, ApiErrorCode.PricingDiscountNotFound);
  }
  const conflict = versionConflict(request, voucherData(voucher)) ?? codeTaken(request, body);
  if (conflict) {
    return conflict;
  }
  if (body.maxUseQuantity != null && body.maxUseQuantity < voucher.usedQuantity) {
    return problem(request, 422, ApiErrorCode.PricingDiscountMaxUsesBelowUsed, {
      args: { usedCount: voucher.usedQuantity },
    });
  }
  const missing = missingSector(request, body.idSectors);
  if (missing) {
    return missing;
  }
  Object.assign(voucher, savedFields(body));
  return savedWithVersion(request, voucherData(voucher));
};

/**
 * `GET /api/catalog/discounts/get-selected-item?idVoucher=…`: the voucher with every field of its form
 * and its version, for the edit popup. 200 with `VoucherSelectedItem`; 401 without a valid access
 * token; 404 `pricing.discount_not_found` for an unknown id.
 */
export const selectedVoucherItemMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const voucher = voucherOf(request.params.get('idVoucher'));
  if (!voucher) {
    return problem(request, 404, ApiErrorCode.PricingDiscountNotFound);
  }
  return of(new HttpResponse({ status: 200, url: request.url, body: selectedOf(voucher) })).pipe(
    delay(300),
  );
};

function selectedOf(voucher: VoucherRow): VoucherSelectedItem {
  const data = voucherData(voucher);
  return structuredClone({ ...data, version: versionOf(data) });
}

/** What `get-selected-item` answers besides the version: the version covers it. */
function voucherData(voucher: VoucherRow): Omit<VoucherSelectedItem, 'version'> {
  return {
    idVoucher: voucher.idVoucher,
    voucherName: voucher.voucherName,
    voucherCode: voucher.voucherCode,
    isCodeRequired: voucher.isCodeRequired,
    validityPeriods: voucher.validityPeriods,
    discountType: voucher.discountType,
    discountValue: voucher.discountValue,
    discountScope: voucher.discountScope,
    serviceCategories: voucher.serviceCategories,
    idSectors: voucher.idSectors,
    maxUseQuantity: voucher.maxUseQuantity,
    isEnabled: voucher.isEnabled,
  };
}

/** 409 when another voucher of the property has the code, in small or capital letters. */
function codeTaken(
  request: HttpRequest<unknown>,
  { voucherCode = null, ...body }: UpdateVoucherBody,
): Observable<never> | null {
  const isTaken = vouchersOfProperty().some(
    (voucher) =>
      voucher.idVoucher !== (body.idVoucher ?? null) &&
      sameTextIgnoringCase(voucher.voucherCode, voucherCode),
  );
  return isTaken
    ? problem(request, 409, ApiErrorCode.PricingDiscountCodeTaken, {
        args: { code: voucherCode?.trim() ?? '' },
      })
    : null;
}

/**
 * As the backend checks it: name and code, with their longest; at least one period without a code to
 * type and at most `MAX_VOUCHER_PERIOD_QUANTITY`, and in each period both days (the last not before the
 * first), at least one day of the week from 1 to 7 and time slots `HH:mm`, the end after the start
 * (none is fine); a known type and scope, a value more than 0 (up to 100 for a percentage and to
 * `MAX_FIXED_DISCOUNT` for an amount), a category or a sector at least unless the scope is `All`, from
 * 1 to `MAX_VOUCHER_USE_QUANTITY` uses or no limit, the switch.
 */
function voucherErrors(body: AddVoucherBody): MockFieldError[] {
  const errors: MockFieldError[] = [];
  const texts = [
    ['voucherName', MAX_VOUCHER_NAME_LENGTH],
    ['voucherCode', MAX_VOUCHER_CODE_LENGTH],
  ] as const;
  for (const [field, max] of texts) {
    const text = body[field]?.trim() ?? '';
    if (!text) {
      errors.push({ field, code: ApiErrorCode.ValidationRequired });
    } else if (text.length > max) {
      errors.push({ field, code: ApiErrorCode.ValidationMaxLength, args: { max } });
    }
  }
  const periods = Array.isArray(body.validityPeriods) ? body.validityPeriods : [];
  if (!periods.length && !body.isCodeRequired) {
    errors.push({ field: 'validityPeriods', code: ApiErrorCode.ValidationRequired });
  }
  if (periods.length > MAX_VOUCHER_PERIOD_QUANTITY) {
    errors.push({ field: 'validityPeriods', code: ApiErrorCode.ValidationInvalid });
  }
  periods.forEach((period, index) => {
    const at = `validityPeriods[${index}]`;
    const isFromDay = isDay(period.validFrom);
    const isToDay = isDay(period.validTo);
    if (!isFromDay) {
      errors.push({ field: `${at}.validFrom`, code: ApiErrorCode.ValidationRequired });
    }
    if (!isToDay) {
      errors.push({ field: `${at}.validTo`, code: ApiErrorCode.ValidationRequired });
    }
    if (isFromDay && isToDay && (period.validTo as string) < (period.validFrom as string)) {
      errors.push({ field: `${at}.validTo`, code: ApiErrorCode.ValidationEndBeforeStart });
    }
    const days = Array.isArray(period.daysOfWeek) ? period.daysOfWeek : [];
    if (!days.length || days.some((day) => !Number.isInteger(day) || day < 1 || day > 7)) {
      errors.push({ field: `${at}.daysOfWeek`, code: ApiErrorCode.ValidationRequired });
    }
    const timeSlots = Array.isArray(period.timeSlots) ? period.timeSlots : [];
    if (
      timeSlots.length > MAX_VOUCHER_TIME_SLOT_QUANTITY ||
      timeSlots.some(
        ({ from, to }) => !from || !to || !TIME.test(from) || !TIME.test(to) || to <= from,
      )
    ) {
      errors.push({ field: `${at}.timeSlots`, code: ApiErrorCode.ValidationInvalid });
    }
  });
  if (!Object.values(DiscountType).includes(body.discountType as DiscountType)) {
    errors.push({ field: 'discountType', code: ApiErrorCode.ValidationInvalid });
  }
  const value = body.discountValue;
  const highest = body.discountType === DiscountType.Percentage ? 100 : MAX_FIXED_DISCOUNT;
  if (typeof value !== 'number' || value <= 0 || value > highest) {
    errors.push({ field: 'discountValue', code: ApiErrorCode.ValidationOutOfRange });
  }
  if (!Object.values(DiscountScope).includes(body.discountScope as DiscountScope)) {
    errors.push({ field: 'discountScope', code: ApiErrorCode.ValidationInvalid });
  }
  const categories = body.serviceCategories ?? [];
  if (categories.some((name) => !Object.values(DiscountServiceCategory).includes(name))) {
    errors.push({ field: 'serviceCategories', code: ApiErrorCode.ValidationInvalid });
  }
  if (
    body.discountScope !== DiscountScope.All &&
    !categories.length &&
    !(body.idSectors ?? []).length
  ) {
    errors.push({ field: 'serviceCategories', code: ApiErrorCode.ValidationRequired });
  }
  const uses = body.maxUseQuantity ?? null;
  if (uses !== null && (!Number.isInteger(uses) || uses < 1 || uses > MAX_VOUCHER_USE_QUANTITY)) {
    errors.push({ field: 'maxUseQuantity', code: ApiErrorCode.ValidationOutOfRange });
  }
  if (typeof body.isEnabled !== 'boolean') {
    errors.push({ field: 'isEnabled', code: ApiErrorCode.ValidationInvalid });
  }
  return errors;
}

function savedFields(
  body: AddVoucherBody | UpdateVoucherBody,
): Omit<VoucherRow, 'propertyId' | 'idVoucher' | 'usedQuantity'> {
  const isAll = body.discountScope === DiscountScope.All;
  return {
    voucherName: body.voucherName?.trim() ?? null,
    voucherCode: body.voucherCode?.trim() ?? null,
    isCodeRequired: body.isCodeRequired ?? true,
    validityPeriods: structuredClone(body.validityPeriods ?? []),
    discountType: body.discountType as DiscountType,
    discountValue: body.discountValue as number,
    discountScope: body.discountScope as DiscountScope,
    serviceCategories: isAll ? [] : [...(body.serviceCategories ?? [])],
    idSectors: isAll ? [] : [...(body.idSectors ?? [])],
    maxUseQuantity: body.maxUseQuantity ?? null,
    isEnabled: body.isEnabled as boolean,
  };
}

function isDay(value: unknown): boolean {
  return typeof value === 'string' && !Number.isNaN(Date.parse(value));
}

/** Types in the order of their enum, numbers by value, texts in natural order; a missing value is last. */
function compare(
  first: string | number | null,
  second: string | number | null,
  sign: number,
): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? 1 : -1;
  }
  if (typeof first === 'number' && typeof second === 'number') {
    return sign * (first - second);
  }
  const types: readonly unknown[] = Object.values(DiscountType);
  return (
    sign *
    (types.includes(first) && types.includes(second)
      ? types.indexOf(first) - types.indexOf(second)
      : naturalOrder(String(first), String(second)))
  );
}
