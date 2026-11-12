import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { AdditionalItem } from '../../../entities/settings/catalog/additional-item';
import { DeleteAdditionalItemsRequest } from '../../../entities/settings/catalog/delete-additional-items-request';
import { DeletePackageItemsRequest } from '../../../entities/settings/catalog/delete-package-items-request';
import { PriceCalculationMethod } from '../../../entities/enums/price-calculation-method';
import { PriceMonthlySetting } from '../../../entities/enums/price-monthly-setting';
import { PriceWeekendSetting } from '../../../entities/enums/price-weekend-setting';
import { DuplicateAdditionalItemsRequest } from '../../../entities/settings/catalog/duplicate-additional-items-request';
import { DuplicatePackageItemsRequest } from '../../../entities/settings/catalog/duplicate-package-items-request';
import {
  MAX_PACKAGE_ADDITIONAL_ITEM_QUANTITY,
  MAX_PACKAGE_ARTICLE_QUANTITY,
  MAX_PACKAGE_PEOPLE_QUANTITY,
  PackageAddItemRequest,
} from '../../../entities/settings/catalog/package-add-item-request';
import { PackageItem } from '../../../entities/settings/catalog/package-item';
import { PackageSelectedItem } from '../../../entities/settings/catalog/package-selected-item';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, forbidden, problem, resourceNotFound } from '../errors/problem.mock';
import { DEMO_PROPERTY, propertyOf } from '../properties/properties.mock';

type PackageRow = PackageItem & { propertyId: number };

const PACKAGE_NAMES = [
  'Ombrellone e 2 lettini',
  'Ombrellone, sdraio e lettino',
  'Ombrellone e lettino',
  'Ombrellone e 2 sdraio',
  'Ombrellone e 3 lettini',
  'Ombrellone famiglia e 4 lettini',
  'Gazebo e 2 lettini',
  'Gazebo e 4 lettini',
  'Tenda e 2 sdraio',
  'Lettino singolo',
  'Prima fila e 2 lettini',
];

const PACKAGES: PackageRow[] = PACKAGE_NAMES.map((packageName, index) => ({
  propertyId: 1,
  idPackage: `8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a${String(index + 1).padStart(2, '0')}`,
  packageName,
}));

/** What the panels saved, by idPackage; a package never saved has only its name. */
const PACKAGE_DETAILS = new Map<string, PackageAddItemRequest>([
  [
    PACKAGES[0].idPackage ?? '',
    Object.assign(new PackageAddItemRequest(), {
      translations: [
        { language: 'it', packageName: PACKAGES[0].packageName },
        { language: 'en', packageName: 'Umbrella and 2 sunbeds' },
      ],
      maxAdditionalItemQuantity: 2,
      maxPeopleQuantity: 4,
      umbrellaQuantity: 1,
      sunbedQuantity: 2,
      includedServices: 'Cassetta di sicurezza',
      idSectors: ['5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01', '5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02'],
      isDefault: true,
      isOnlineEnabled: true,
      reducedSeasonalPrice: {
        dateFrom: '2026-06-01T00:00:00.000Z',
        dateTo: '2026-06-30T00:00:00.000Z',
        reducedSeasonalPriceName: 'Giugno',
      },
      sectorPrices: [
        {
          idSector: '5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01',
          seasonalHalfDayPrice: 600,
          seasonalPrice: 1100,
        },
      ],
      calculationMethod: PriceCalculationMethod.Daily,
      monthlySetting: PriceMonthlySetting.EffectiveMonthDuration,
      weekendSetting: PriceWeekendSetting.SaturdaySunday,
    }),
  ],
]);

const MAX_PAGE_SIZE = 100;
const SORT_FIELDS = ['packageName'] as const;

/**
 * `POST /api/catalog/packages-list`: one page of the packages of the requested property, in their
 * own order unless sorted. 401 without a valid access token; 403 for a missing or unknown property.
 */
export const packageListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
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
  const found = PACKAGES.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search || row.packageName?.toLowerCase().includes(search)),
  ).map((row) => onlyFieldsOf(PackageItem, row));
  const field = SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }
  const answer: Page<PackageItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/**
 * `POST /api/catalog/delete-package-item`: deletes the given packages, all or nothing. 204 with no
 * body; 404 when an id is not a package of the property; 401 / 403 as the list.
 */
export const deletePackageItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DeletePackageItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const packages = packagesOf(property.id, body.idPackages);
  if (!packages) {
    return resourceNotFound(request);
  }
  for (const item of packages) {
    PACKAGES.splice(PACKAGES.indexOf(item), 1);
    PACKAGE_DETAILS.delete(item.idPackage ?? '');
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/catalog/duplicate-package-item`: one copy of each given package at the end of the list,
 * all or nothing, named by the backend ("X (copia)", then "X (copia 2)"…). 204 with no body;
 * 404 when an id is not a package of the property; 401 / 403 as the list.
 */
export const duplicatePackageItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DuplicatePackageItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const packages = packagesOf(property.id, body.idPackages);
  if (!packages) {
    return resourceNotFound(request);
  }
  for (const item of packages) {
    const copy: PackageRow = {
      propertyId: item.propertyId,
      idPackage: crypto.randomUUID(),
      packageName: copyName(item),
    };
    const detail = PACKAGE_DETAILS.get(item.idPackage ?? '');
    if (detail) {
      PACKAGE_DETAILS.set(copy.idPackage ?? '', {
        ...structuredClone(detail),
        translations: detail.translations.map((translation) =>
          translation.language === 'it'
            ? { ...translation, packageName: copy.packageName }
            : { ...translation },
        ),
        isDefault: false,
        isDefaultOnline: false,
      });
    }
    PACKAGES.push(copy);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

function copyName(item: PackageRow): string {
  const taken = new Set(
    PACKAGES.filter((row) => row.propertyId === item.propertyId).map((row) =>
      row.packageName?.toLowerCase(),
    ),
  );
  let copy = `${item.packageName} (copia)`;
  for (let number = 2; taken.has(copy.toLowerCase()); number++) {
    copy = `${item.packageName} (copia ${number})`;
  }
  return copy;
}

function packagesOf(propertyId: number, idPackages: unknown): PackageRow[] | null {
  const ids = Array.isArray(idPackages) ? [...new Set(idPackages)] : [];
  const packages = ids
    .map((id) => PACKAGES.find((row) => row.idPackage === id && row.propertyId === propertyId))
    .filter((item): item is PackageRow => item !== undefined);
  return ids.length && packages.length === ids.length ? packages : null;
}

function compare(first: string | null, second: string | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? -1 : 1;
  }
  return first.localeCompare(second, 'it', { sensitivity: 'base', numeric: true });
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}

type CreatePackageBody = Partial<PackageAddItemRequest>;

/**
 * `POST /api/catalog/packages/create-package` with `PackageAddItemRequest`, the property from the
 * access token: the new package goes into the list, with the name in the language of
 * `Accept-Language`. 204 without a body; 401 without a valid access token; 400 with one error per
 * wrong field (see `packageErrors`).
 */
export const createPackageMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreatePackageBody;
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = packageErrors(body, language);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const main = body.translations?.find((translation) => translation.language === language);
  const row: PackageRow = {
    propertyId: DEMO_PROPERTY.id,
    idPackage: crypto.randomUUID(),
    packageName: main?.packageName?.trim() ?? null,
  };
  PACKAGES.push(row);
  PACKAGE_DETAILS.set(
    row.idPackage ?? '',
    structuredClone(onlyFieldsOf(PackageAddItemRequest, body as PackageAddItemRequest)),
  );
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * The name in the language of the request (the user's), as the panel requires; the quantities, when
 * given, within the choices of their dropdowns; calculation method, monthly and weekend settings,
 * when given, among the values of their enums; the reduced seasonal price, when given, with its
 * name and its days in order.
 */
function packageErrors(body: CreatePackageBody, language: string): MockFieldError[] {
  const errors: MockFieldError[] = [];
  const translations = Array.isArray(body.translations) ? body.translations : [];
  const index = translations.findIndex((translation) => translation.language === language);
  const main = translations[index];
  if (typeof main?.packageName !== 'string' || !main.packageName.trim()) {
    errors.push({
      field: `translations[${Math.max(index, 0)}].packageName`,
      code: 'validation.required',
    });
  }
  const limits = [
    ['maxAdditionalItemQuantity', MAX_PACKAGE_ADDITIONAL_ITEM_QUANTITY],
    ['maxPeopleQuantity', MAX_PACKAGE_PEOPLE_QUANTITY],
    ['umbrellaQuantity', MAX_PACKAGE_ARTICLE_QUANTITY],
    ['sunbedQuantity', MAX_PACKAGE_ARTICLE_QUANTITY],
    ['deckchairQuantity', MAX_PACKAGE_ARTICLE_QUANTITY],
    ['directorChairQuantity', MAX_PACKAGE_ARTICLE_QUANTITY],
    ['cabinQuantity', MAX_PACKAGE_ARTICLE_QUANTITY],
    ['tentQuantity', MAX_PACKAGE_ARTICLE_QUANTITY],
  ] as const;
  for (const [field, last] of limits) {
    const quantity = body[field];
    if (quantity != null && !(Number.isInteger(quantity) && quantity >= 1 && quantity <= last)) {
      errors.push({ field, code: 'validation.invalid' });
    }
  }
  if (
    body.calculationMethod != null &&
    !Object.values(PriceCalculationMethod).includes(body.calculationMethod)
  ) {
    errors.push({ field: 'calculationMethod', code: 'validation.invalid' });
  }
  if (
    body.monthlySetting != null &&
    !Object.values(PriceMonthlySetting).includes(body.monthlySetting)
  ) {
    errors.push({ field: 'monthlySetting', code: 'validation.invalid' });
  }
  if (
    body.weekendSetting != null &&
    !Object.values(PriceWeekendSetting).includes(body.weekendSetting)
  ) {
    errors.push({ field: 'weekendSetting', code: 'validation.invalid' });
  }
  const reduced = body.reducedSeasonalPrice;
  if (reduced) {
    if (!reduced.reducedSeasonalPriceName?.trim()) {
      errors.push({
        field: 'reducedSeasonalPrice.reducedSeasonalPriceName',
        code: 'validation.required',
      });
    }
    if (!reduced.dateFrom || !reduced.dateTo) {
      errors.push({ field: 'reducedSeasonalPrice.dateFrom', code: 'validation.required' });
    } else if (reduced.dateTo < reduced.dateFrom) {
      errors.push({ field: 'reducedSeasonalPrice.dateTo', code: 'validation.end_before_start' });
    }
  }
  return errors;
}

/** The id at the end of `/api/catalog/packages/{idPackage}`. */
function idPackageOf(request: HttpRequest<unknown>): string {
  return decodeURIComponent(request.url.slice(request.url.lastIndexOf('/') + 1));
}

/** The package of the token's property, null when it is not one of them. */
function packageOf(idPackage: string): PackageRow | null {
  return (
    PACKAGES.find((row) => row.idPackage === idPackage && row.propertyId === DEMO_PROPERTY.id) ??
    null
  );
}

/**
 * `GET /api/catalog/packages/{idPackage}`, the property from the access token: everything the
 * package has, as the panels send it; a package never saved has only its name in Italian. 200;
 * 401 without a valid access token; 404 `resource.not_found` for a package that is not one of its.
 */
export const selectedPackageMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = packageOf(idPackageOf(request));
  if (!row) {
    return resourceNotFound(request);
  }
  const detail =
    PACKAGE_DETAILS.get(row.idPackage ?? '') ??
    Object.assign(new PackageAddItemRequest(), {
      translations: [{ language: 'it', packageName: row.packageName }],
    });
  const answer: PackageSelectedItem = { ...structuredClone(detail), idPackage: row.idPackage };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(300));
};

/**
 * `POST /api/catalog/packages/update-package` with `PackageAddItemRequest` and `idPackage`, the
 * property from the access token: the package takes all the values sent, its name in the list the
 * one in the language of `Accept-Language`. 204 without a body; 401 without a valid access token;
 * 404 `resource.not_found` for a package that is not one of its; 400 as `createPackageMock`.
 */
export const updatePackageMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreatePackageBody & { idPackage?: unknown };
  const row = typeof body.idPackage === 'string' ? packageOf(body.idPackage) : null;
  if (!row) {
    return resourceNotFound(request);
  }
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = packageErrors(body, language);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const main = body.translations?.find((translation) => translation.language === language);
  row.packageName = main?.packageName?.trim() ?? row.packageName;
  PACKAGE_DETAILS.set(
    row.idPackage ?? '',
    structuredClone(onlyFieldsOf(PackageAddItemRequest, body as PackageAddItemRequest)),
  );
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

type AdditionalItemRow = AdditionalItem & { propertyId: number };

const ADDITIONAL_ITEM_NAMES = [
  'Lettino',
  'Sdraio',
  'Sedia regista',
  'Ombrellone aggiuntivo',
  'Telo mare',
  'Cassetta di sicurezza',
  'Doccia calda',
  'Posto auto',
  'Cabina giornaliera',
  'Tenda',
  'Lettino extra famiglia',
];

const ADDITIONAL_ITEMS: AdditionalItemRow[] = ADDITIONAL_ITEM_NAMES.map(
  (additionalItemName, index) => ({
    propertyId: 1,
    idAdditionalItem: `4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a${String(index + 1).padStart(2, '0')}`,
    additionalItemName,
  }),
);

const ADDITIONAL_ITEM_SORT_FIELDS = ['additionalItemName'] as const;

/**
 * `POST /api/catalog/additional-services-articles-list`: one page of the additional items/services of
 * the requested property, in their own order unless sorted. 401 without a valid access token; 403
 * for a missing or unknown property.
 */
export const additionalItemListMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
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
  const found = ADDITIONAL_ITEMS.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search || row.additionalItemName?.toLowerCase().includes(search)),
  ).map((row) => onlyFieldsOf(AdditionalItem, row));
  const field = ADDITIONAL_ITEM_SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }
  const answer: Page<AdditionalItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/**
 * `POST /api/catalog/delete-additional-services-article-item`: deletes the given additional
 * items/services, all or nothing. 204 with no body; 404 when an id is not an additional item of the
 * property; 401 / 403 as the list.
 */
export const deleteAdditionalItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DeleteAdditionalItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const items = additionalItemsOf(property.id, body.idAdditionalItems);
  if (!items) {
    return resourceNotFound(request);
  }
  for (const item of items) {
    ADDITIONAL_ITEMS.splice(ADDITIONAL_ITEMS.indexOf(item), 1);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/catalog/duplicate-additional-services-article-item`: one copy of each given additional
 * item/service at the end of the list, all or nothing, named by the backend ("X (copia)", then
 * "X (copia 2)"…). 204 with no body; 404 when an id is not an additional item of the property;
 * 401 / 403 as the list.
 */
export const duplicateAdditionalItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DuplicateAdditionalItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const items = additionalItemsOf(property.id, body.idAdditionalItems);
  if (!items) {
    return resourceNotFound(request);
  }
  for (const item of items) {
    ADDITIONAL_ITEMS.push({
      propertyId: item.propertyId,
      idAdditionalItem: crypto.randomUUID(),
      additionalItemName: additionalItemCopyName(item),
    });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

function additionalItemCopyName(item: AdditionalItemRow): string {
  const taken = new Set(
    ADDITIONAL_ITEMS.filter((row) => row.propertyId === item.propertyId).map((row) =>
      row.additionalItemName?.toLowerCase(),
    ),
  );
  let copy = `${item.additionalItemName} (copia)`;
  for (let number = 2; taken.has(copy.toLowerCase()); number++) {
    copy = `${item.additionalItemName} (copia ${number})`;
  }
  return copy;
}

function additionalItemsOf(
  propertyId: number,
  idAdditionalItems: unknown,
): AdditionalItemRow[] | null {
  const ids = Array.isArray(idAdditionalItems) ? [...new Set(idAdditionalItems)] : [];
  const items = ids
    .map((id) =>
      ADDITIONAL_ITEMS.find((row) => row.idAdditionalItem === id && row.propertyId === propertyId),
    )
    .filter((item): item is AdditionalItemRow => item !== undefined);
  return ids.length && items.length === ids.length ? items : null;
}
