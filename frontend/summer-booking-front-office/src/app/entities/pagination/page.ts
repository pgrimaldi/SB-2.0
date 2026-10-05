export const DEFAULT_PAGE_SIZE = 10;

/** Values are the .NET enum names the API expects. */
export type SortDirection = 'Ascending' | 'Descending';

/**
 * `page` starts from 1. '' means none: in `search` the server decides which fields it searches, in
 * `sortField` it uses its own order. Same shape as the `TablePageRequest` of `app-table`, so a list
 * API can be given to the table as its `load`.
 */
export interface PageRequest {
  page: number;
  pageSize: number;
  search: string;
  sortField: string;
  sortDirection: SortDirection;
}

/** Same shape as the `TablePage` of `app-table`; `total` counts the rows of all the pages. */
export interface Page<T> {
  total: number;
  rows: T[];
}
