/** Rows shown in a page when nothing else is chosen. */
export const DEFAULT_PAGE_SIZE = 10;

/** Query of a paginated API: `page` starts from 1. */
export interface PageRequest {
  page: number;
  pageSize: number;
}

/** Answer of a paginated API: the rows of the requested page and how many rows there are in all. */
export interface Page<T> {
  total: number;
  rows: T[];
}
