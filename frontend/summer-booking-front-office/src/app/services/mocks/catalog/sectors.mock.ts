import { HttpEvent, HttpHeaders, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_SEARCH_MAX_LENGTH,
} from '../../../components/shared/ui/tables/table/table';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { Page, PageRequest, SortDirection } from '../../../entities/pagination/page';
import { Sector } from '../../../entities/settings/rows-and-sectors/sector';
import {
  MAX_SECTOR_NAME_LENGTH,
  SectorItem,
} from '../../../entities/settings/rows-and-sectors/sector-item';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';
import { sameTextIgnoringCase } from '../../../components/shared/forms/text-comparison';
import { savedWithVersion, versionConflict, versionOf } from '../versions/versions.mock';
import { sectorLimits } from './catalog.mock';

interface SectorUsage {
  mapSpots: number;
  priceLists: number;
  stayLimits: number;
  discounts: number;
  services: number;
  automaticPrices: number;
}

type SectorRow = Omit<SectorItem, 'version'> & {
  propertyId: number;
  sortOrder: number;
  usage?: Partial<SectorUsage>;
};

const SECTOR_NAMES = [
  'Fila 1',
  'Fila 2',
  'Fila 3',
  'Fila 4',
  'Fila 5',
  'Fila 6',
  'Fila 7',
  'Fila 8',
  'Settore Est',
  'Settore Ovest',
  'Gazebo',
  'Zona relax',
];

const SECTORS: SectorRow[] = SECTOR_NAMES.map((sectorName, index) => ({
  propertyId: 1,
  idSector: `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a${String(index + 1).padStart(2, '0')}`,
  sectorName,
  sortOrder: index,
  ...(index === 0 ? { usage: { mapSpots: 14, priceLists: 1 } } : {}),
}));

const SORT_FIELDS = ['sectorName'] as const;
const SORT_DIRECTIONS: readonly SortDirection[] = ['Ascending', 'Descending'];

/**
 * `POST /api/catalog/sectors-list`: one page of the sectors (rows) of the property of the token, in
 * their own order unless sorted, each with its version. 400 `validation.invalid` on `page`, `pageSize`, `sortField`,
 * `sortDirection` or `filters` out of the table's rules and `validation.max_length` on a `search`
 * over 100 characters; 401 without a valid access token.
 */
export const sectorListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const errors = tableRequestErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const property = DEMO_PROPERTY;
  const { page = 1, pageSize = TABLE_PAGE_SIZE_OPTIONS[0] } = body;
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = SECTORS.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search || row.sectorName?.trim().toLowerCase().includes(search)),
  )
    .sort((first, second) => first.sortOrder - second.sortOrder)
    .map(listRowOf);
  const field = SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }
  const answer: Page<SectorItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/**
 * `POST /api/catalog/sectors/sector-combobox` without a body: every sector of the property, in its order,
 * as `{ id, value }`. 401 without a valid access token.
 */
export const sectorComboboxMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const property = DEMO_PROPERTY;
  const items: ComboboxItem[] = SECTORS.filter((row) => row.propertyId === property.id)
    .sort((first, second) => first.sortOrder - second.sortOrder)
    .map((row) => ({ id: row.idSector ?? '', value: row.sectorName ?? '' }));
  return of(new HttpResponse({ status: 200, url: request.url, body: items })).pipe(delay(150));
};

/**
 * `POST /api/catalog/sectors` with `name`: the new sector goes at the end of the order. 400 with
 * validation.required / validation.max_length on name; 409 `catalog.sector_name_taken` for a name
 * the property has already (case-insensitive). 201 with the sector and its `Location`; 401 without
 * a valid access token.
 */
export const addSectorMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const name = (request.body as { name?: unknown } | null)?.name;
  const nameError = sectorNameError(request, name);
  if (nameError) {
    return nameError;
  }
  const property = DEMO_PROPERTY;
  const sectorName = String(name).trim();
  if (isNameTaken(property.id, sectorName)) {
    return problem(request, 409, ApiErrorCode.CatalogSectorNameTaken);
  }
  const sector: SectorRow = {
    propertyId: property.id,
    idSector: crypto.randomUUID(),
    sectorName,
    sortOrder: nextSortOrder(property.id),
  };
  SECTORS.push(sector);
  return of(
    new HttpResponse({
      status: 201,
      url: request.url,
      headers: new HttpHeaders({ Location: `${request.url}/${sector.idSector}` }),
      body: sectorOf(sector),
    }),
  ).pipe(delay(600));
};

/**
 * `PUT /api/catalog/sectors/{id}` with `name`: the sector is renamed. The name is checked first (the
 * same errors as the add, the sector's own name does not count as taken), then 404
 * `catalog.sector_not_found` for an id that is not a sector of the property, then 409
 * `resource.version_conflict` when `If-Match` is not the version of its row. 200 with the sector and
 * the new version in `ETag`; 401 without a valid access token.
 */
export const updateSectorMock = (
  request: HttpRequest<unknown>,
  idSector: string,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const name = (request.body as { name?: unknown } | null)?.name;
  const nameError = sectorNameError(request, name);
  if (nameError) {
    return nameError;
  }
  const property = DEMO_PROPERTY;
  const sector = sectorOfProperty(property.id, idSector);
  if (!sector) {
    return problem(request, 404, ApiErrorCode.CatalogSectorNotFound);
  }
  const conflict = versionConflict(request, sectorData(sector));
  if (conflict) {
    return conflict;
  }
  const sectorName = String(name).trim();
  if (isNameTaken(property.id, sectorName, sector)) {
    return problem(request, 409, ApiErrorCode.CatalogSectorNameTaken);
  }
  sector.sectorName = sectorName;
  return savedWithVersion(request, sectorData(sector), sectorOf(sector));
};

/**
 * `POST /api/catalog/sectors/bulk-delete` with `{ idSectors }`, one id or more: all or nothing. If
 * one id is not a
 * sector of the property nothing is deleted (404 `catalog.sector_not_found`); a sector still used
 * stops it too (409 `catalog.sector_in_use` with the name of that sector and its uses). Repeated ids
 * count once. 204 without a body; 401 without a valid access token.
 */
export const deleteSectorsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idSectors?: unknown };
  const idSectorsError = emptyIdSectorsError(request, body.idSectors);
  if (idSectorsError) {
    return idSectorsError;
  }
  const sectors = sectorsOf(DEMO_PROPERTY.id, body.idSectors);
  if (!sectors) {
    return problem(request, 404, ApiErrorCode.CatalogSectorNotFound);
  }
  const used = sectors.find((sector) => Object.values(usageOf(sector)).some((count) => count > 0));
  if (used) {
    return problem(request, 409, ApiErrorCode.CatalogSectorInUse, {
      args: { sectorName: used.sectorName ?? '', ...usageOf(used) },
    });
  }
  for (const sector of sectors) {
    SECTORS.splice(SECTORS.indexOf(sector), 1);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

function usageOf(sector: SectorRow): SectorUsage {
  return {
    mapSpots: 0,
    priceLists: 0,
    stayLimits: 0,
    discounts: 0,
    ...sector.usage,
    ...sectorLimits(sector.idSector ?? ''),
  };
}

/**
 * `POST /api/catalog/sectors/bulk-duplicate` with `{ idSectors }`: all or nothing, like the delete.
 * The copies go at the end of the order; the name is chosen by the backend and is unique in the
 * property: the mock invents "Fila 2 (copia)", then "Fila 2 (copia 2)" and so on. A copy is not used
 * anywhere yet. 204 without a body; 401 without a valid access token.
 */
export const duplicateSectorsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idSectors?: unknown };
  const idSectorsError = emptyIdSectorsError(request, body.idSectors);
  if (idSectorsError) {
    return idSectorsError;
  }
  const sectors = sectorsOf(DEMO_PROPERTY.id, body.idSectors);
  if (!sectors) {
    return problem(request, 404, ApiErrorCode.CatalogSectorNotFound);
  }
  for (const sector of sectors) {
    SECTORS.push({
      propertyId: sector.propertyId,
      idSector: crypto.randomUUID(),
      sectorName: copyName(sector),
      sortOrder: nextSortOrder(sector.propertyId),
    });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** `idSectors` missing or empty: `validation.required`, as in the requests doc. */
function emptyIdSectorsError(
  request: HttpRequest<unknown>,
  idSectors: unknown,
): Observable<never> | null {
  return Array.isArray(idSectors) && idSectors.length
    ? null
    : problem(request, 400, ApiErrorCode.ValidationFailed, {
        errors: [{ field: 'idSectors', code: ApiErrorCode.ValidationRequired }],
      });
}

/** As the backend's validator: not blank, at most 100 characters before the spaces are removed. */
function sectorNameError(request: HttpRequest<unknown>, name: unknown): Observable<never> | null {
  const text = typeof name === 'string' ? name : '';
  if (text.trim() && text.length <= MAX_SECTOR_NAME_LENGTH) {
    return null;
  }
  return problem(request, 400, ApiErrorCode.ValidationFailed, {
    errors: [
      text.trim()
        ? {
            field: 'name',
            code: ApiErrorCode.ValidationMaxLength,
            args: { max: MAX_SECTOR_NAME_LENGTH, min: 0 },
          }
        : { field: 'name', code: ApiErrorCode.ValidationRequired },
    ],
  });
}

function listRowOf(row: SectorRow): SectorItem {
  const data = sectorData(row);
  return { ...data, version: versionOf(data) };
}

/** A row of `sectors-list` besides the version: the version covers it. */
function sectorData(row: SectorRow): Omit<SectorItem, 'version'> {
  return { idSector: row.idSector, sectorName: row.sectorName };
}

function sectorOf(row: SectorRow): Sector {
  return { id: row.idSector, name: row.sectorName, sortOrder: row.sortOrder };
}

function nextSortOrder(propertyId: number): number {
  const orders = SECTORS.filter((row) => row.propertyId === propertyId).map((row) => row.sortOrder);
  return orders.length ? Math.max(...orders) + 1 : 0;
}

function isNameTaken(propertyId: number, sectorName: string, renamed?: SectorRow): boolean {
  return SECTORS.some(
    (row) =>
      row !== renamed &&
      row.propertyId === propertyId &&
      sameTextIgnoringCase(row.sectorName, sectorName),
  );
}

function sectorOfProperty(propertyId: number, idSector: string): SectorRow | undefined {
  return SECTORS.find((row) => row.idSector === idSector && row.propertyId === propertyId);
}

function sectorsOf(propertyId: number, idSectors: unknown): SectorRow[] | null {
  const ids = Array.isArray(idSectors) ? [...new Set<unknown>(idSectors)] : [];
  const sectors = ids
    .map((id) => (typeof id === 'string' ? sectorOfProperty(propertyId, id) : undefined))
    .filter((sector): sector is SectorRow => sector !== undefined);
  return ids.length && sectors.length === ids.length ? sectors : null;
}

function copyName(sector: SectorRow): string {
  const taken = new Set(
    SECTORS.filter((row) => row.propertyId === sector.propertyId).map((row) =>
      row.sectorName?.trim().toLowerCase(),
    ),
  );
  for (let number = 1; ; number++) {
    const suffix = number === 1 ? ' (copia)' : ` (copia ${number})`;
    const copy = `${(sector.sectorName ?? '').slice(0, MAX_SECTOR_NAME_LENGTH - suffix.length)}${suffix}`;
    if (!taken.has(copy.trim().toLowerCase())) {
      return copy;
    }
  }
}

function compare(first: string | null, second: string | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? -1 : 1;
  }
  return first.trim().localeCompare(second.trim(), 'it', { sensitivity: 'base', numeric: true });
}

/** The common rules of the table requests, and the search at most 100 characters. */
function tableRequestErrors(body: Partial<PageRequest>): MockFieldError[] {
  const wrong = [
    { field: 'page', isWrong: !Number.isInteger(body.page) || (body.page ?? 0) < 1 },
    { field: 'pageSize', isWrong: !TABLE_PAGE_SIZE_OPTIONS.includes(body.pageSize ?? 0) },
    {
      field: 'sortField',
      isWrong: !!body.sortField && !SORT_FIELDS.some((name) => name === body.sortField),
    },
    {
      field: 'sortDirection',
      isWrong: !SORT_DIRECTIONS.some((direction) => direction === body.sortDirection),
    },
    { field: 'filters', isWrong: !Array.isArray(body.filters) || body.filters.length > 0 },
  ];
  const errors: MockFieldError[] = wrong
    .filter(({ isWrong }) => isWrong)
    .map(({ field }) => ({ field, code: ApiErrorCode.ValidationInvalid }));
  if (typeof body.search === 'string' && body.search.length > TABLE_SEARCH_MAX_LENGTH) {
    errors.push({
      field: 'search',
      code: ApiErrorCode.ValidationMaxLength,
      args: { max: TABLE_SEARCH_MAX_LENGTH, min: 0 },
    });
  }
  return errors;
}
