export const DEFAULT_PAGE_SIZE = 10;

/** Values are the .NET enum names the API expects. */
export type SortDirection = 'Ascending' | 'Descending';

/**
 * `page` starts from 1. '' means none: in `search` the server decides which fields it searches, in
 * `sortField` it uses its own order.
 */
export interface PageRequest {
  page: number;
  pageSize: number;
  search: string;
  sortField: string;
  sortDirection: SortDirection;
}

/** `total` counts the rows of all the pages. */
export interface Page<T> {
  total: number;
  rows: T[];
}
