import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { DeletePackageItemsRequest } from '../../../entities/settings/catalog/delete-package-items-request';
import { PriceCalculationMethod } from '../../../entities/enums/price-calculation-method';
import { PriceMonthlySetting } from '../../../entities/enums/price-monthly-setting';
import { PriceWeekendSetting } from '../../../entities/enums/price-weekend-setting';
import { DuplicatePackageItemsRequest } from '../../../entities/settings/catalog/duplicate-package-items-request';
import {
  MAX_PACKAGE_ADDITIONAL_ITEM_QUANTITY,
  MAX_PACKAGE_ARTICLE_QUANTITY,
  MAX_PACKAGE_PEOPLE_QUANTITY,
  PackageAddItemRequest,
} from '../../../entities/settings/catalog/package-add-item-request';
import { PackageItem } from '../../../entities/settings/catalog/package-item';
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
    PACKAGES.push({
      propertyId: item.propertyId,
      idPackage: crypto.randomUUID(),
      packageName: copyName(item),
    });
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
 * `POST /api/catalog/price-lists` with `PackageAddItemRequest`, the property from the access token:
 * the new package goes into the list, with the name in the language of `Accept-Language`. 201 with
 * the new row; 401 without a valid access token; 400 with one error per wrong field (see
 * `packageErrors`).
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
  return of(
    new HttpResponse({ status: 201, url: request.url, body: onlyFieldsOf(PackageItem, row) }),
  ).pipe(delay(600));
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
