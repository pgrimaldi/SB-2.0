import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { SectorItem } from '../../../entities/settings/rows-and-sectors/sector-item';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { forbidden } from '../errors/problem.mock';
import { propertyOf } from '../properties/properties.mock';

type SectorRow = SectorItem & { propertyId: number };

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

function compare(first: string | null, second: string | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? -1 : 1;
  }
  return first.localeCompare(second, 'it', { sensitivity: 'base', numeric: true });
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}
