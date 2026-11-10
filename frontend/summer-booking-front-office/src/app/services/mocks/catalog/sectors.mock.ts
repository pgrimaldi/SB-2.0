import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { DeleteSectorItemsRequest } from '../../../entities/settings/rows-and-sectors/delete-sector-items-request';
import { DuplicateSectorItemsRequest } from '../../../entities/settings/rows-and-sectors/duplicate-sector-items-request';
import { SectorItem } from '../../../entities/settings/rows-and-sectors/sector-item';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { forbidden, problem, resourceNotFound } from '../errors/problem.mock';
import { propertyOf } from '../properties/properties.mock';

interface SectorUsage {
  mapSpots: number;
  priceLists: number;
  stayLimits: number;
  discounts: number;
}

type SectorRow = SectorItem & { propertyId: number; usage?: SectorUsage };

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
  ...(index === 0 ? { usage: { mapSpots: 14, priceLists: 1, stayLimits: 0, discounts: 0 } } : {}),
}));

const MAX_PAGE_SIZE = 100;
const SORT_FIELDS = ['sectorName'] as const;

/**
 * `POST /api/catalog/sectors-list`: one page of the sectors (rows) of the requested property, in
 * their own order unless sorted. 401 without a valid access token; 403 for a missing or unknown
 * property.
 */
export const sectorListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
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
  const found = SECTORS.filter(
    (row) =>
      row.propertyId === property.id && (!search || row.sectorName?.toLowerCase().includes(search)),
  ).map((row) => onlyFieldsOf(SectorItem, row));
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
 * `POST /api/catalog/delete-sector-item` with `DeleteSectorItemsRequest`: all or nothing. If one id
 * is not a sector of the property nothing is deleted (404 `resource.not_found`); a sector still used
 * by the map, a price list, a stay limit or a discount stops it too (409 `catalog.sector_in_use`, the
 * mock uses "Fila 1"). Repeated ids count once. 204 without a body; 401 without a valid access token;
 * 403 for an unknown property.
 */
export const deleteSectorItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DeleteSectorItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const sectors = sectorsOf(property.id, body.idSectors);
  if (!sectors) {
    return resourceNotFound(request);
  }
  const used = sectors.find((sector) => sector.usage);
  if (used?.usage) {
    return problem(request, 409, 'catalog.sector_in_use', {
      title: 'Sector in use',
      args: { sectorName: used.sectorName ?? '', ...used.usage },
    });
  }
  for (const sector of sectors) {
    SECTORS.splice(SECTORS.indexOf(sector), 1);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/catalog/duplicate-sector-item` with `DuplicateSectorItemsRequest`: all or nothing, like
 * the delete. The copies go at the end of the order; the name is chosen by the backend and is unique
 * in the property: the mock invents "Fila 2 (copia)", then "Fila 2 (copia 2)" and so on. A copy is
 * not used anywhere yet.
 */
export const duplicateSectorItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DuplicateSectorItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const sectors = sectorsOf(property.id, body.idSectors);
  if (!sectors) {
    return resourceNotFound(request);
  }
  for (const sector of sectors) {
    SECTORS.push({
      propertyId: sector.propertyId,
      idSector: crypto.randomUUID(),
      sectorName: copyName(sector),
    });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

function sectorsOf(propertyId: number, idSectors: unknown): SectorRow[] | null {
  const ids = Array.isArray(idSectors) ? [...new Set(idSectors)] : [];
  const sectors = ids
    .map((id) => SECTORS.find((row) => row.idSector === id && row.propertyId === propertyId))
    .filter((sector): sector is SectorRow => sector !== undefined);
  return ids.length && sectors.length === ids.length ? sectors : null;
}

function copyName(sector: SectorRow): string {
  const taken = new Set(
    SECTORS.filter((row) => row.propertyId === sector.propertyId).map((row) =>
      row.sectorName?.toLowerCase(),
    ),
  );
  let copy = `${sector.sectorName} (copia)`;
  for (let number = 2; taken.has(copy.toLowerCase()); number++) {
    copy = `${sector.sectorName} (copia ${number})`;
  }
  return copy;
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
