import { PageFilter, SortDirection } from '../../pagination/page';

export interface AuditLogFiltersRequest {
  search: string;
  filters: PageFilter[];
  sortField: string;
  sortDirection: SortDirection;
}
