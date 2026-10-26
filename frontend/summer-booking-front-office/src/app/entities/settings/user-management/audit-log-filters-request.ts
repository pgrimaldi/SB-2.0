import { PageFilter, SortDirection } from '../../pagination/page';

export interface AuditLogFiltersRequest {
  idProperty: string;
  search: string;
  filters: PageFilter[];
  sortField: string;
  sortDirection: SortDirection;
}
