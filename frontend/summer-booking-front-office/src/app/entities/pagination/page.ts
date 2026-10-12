export const DEFAULT_PAGE_SIZE = 10;

export type SortDirection = 'Ascending' | 'Descending';

export interface PageRequest {
  page: number;
  pageSize: number;
  search: string;
  sortField: string;
  sortDirection: SortDirection;
}

export interface Page<T> {
  total: number;
  rows: T[];
}
