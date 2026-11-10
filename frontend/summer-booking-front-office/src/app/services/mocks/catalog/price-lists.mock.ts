import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { DeletePackageItemsRequest } from '../../../entities/settings/catalog/delete-package-items-request';
import { DuplicatePackageItemsRequest } from '../../../entities/settings/catalog/duplicate-package-items-request';
import { PackageItem } from '../../../entities/settings/catalog/package-item';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { forbidden, resourceNotFound } from '../errors/problem.mock';
import { propertyOf } from '../properties/properties.mock';

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
