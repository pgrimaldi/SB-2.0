import { PageFilter, SortDirection } from '../../pagination/page';

/** Characters of the search of the log at most, as the backend (`AuditQueryRules.SearchMaxLength`). */
export const MAX_AUDIT_SEARCH_LENGTH = 100;

export interface AuditLogFiltersRequest {
  search: string;
  filters: PageFilter[];
  sortField: string;
  sortDirection: SortDirection;
}
