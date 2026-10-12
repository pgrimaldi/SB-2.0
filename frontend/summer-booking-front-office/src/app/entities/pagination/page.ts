export const DEFAULT_PAGE_SIZE = 10;

export type SortDirection = 'Ascending' | 'Descending';

export interface PageFilter {
  field: string;
  value: string;
}

export interface PageRequest {
  idProperty: string;
  page: number;
  pageSize: number;
  search: string;
  filters: PageFilter[];
  sortField: string;
  sortDirection: SortDirection;
}

export interface Page<T> {
  total: number;
  rows: T[];
}
