/** Rows shown in a page when nothing else is chosen. */
export const DEFAULT_PAGE_SIZE = 10;

/**
 * Query of a paginated API: `page` starts from 1; `search` is the text searched in the list ('' for
 * none: the server decides in which fields). Same shape as the `TablePageRequest` of `app-table`, so a
 * list API can be given to the table as its `load`.
 */
export interface PageRequest {
  page: number;
  pageSize: number;
  search: string;
}

/** Answer of a paginated API: the rows of the requested page and how many rows there are in all. */
// Same shape as the `TablePage` of `app-table`.
export interface Page<T> {
  total: number;
  rows: T[];
}
