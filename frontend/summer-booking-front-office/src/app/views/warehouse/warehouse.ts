import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { EMPTY, catchError, of, switchMap } from 'rxjs';
import { ManagementFiltersBehaviour } from '../../behaviours/management/management-filters.behaviour';
import { Table, TableColumn } from '../../components/shared/ui/tables/table/table';
import { DEFAULT_PAGE_SIZE, Page } from '../../entities/pagination/page';
import { WarehouseItem } from '../../entities/warehouse/warehouse-item';
import { WarehouseService } from '../../services/api/warehouse/warehouse.service';

const NO_ITEMS: Page<WarehouseItem> = { total: 0, rows: [] };

/** Warehouse of the property: articles with total and today's available quantity, page by page. */
@Component({
  selector: 'app-warehouse',
  imports: [Table],
  templateUrl: './warehouse.html',
  styleUrl: './warehouse.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Warehouse {
  private readonly filters = inject(ManagementFiltersBehaviour);
  private readonly warehouseService = inject(WarehouseService);

  protected readonly pageSize = DEFAULT_PAGE_SIZE;
  /** Current page; back to the first one whenever a filter of the header changes. */
  protected readonly page = linkedSignal({
    source: () => [this.filters.request(), this.filters.search()],
    computation: () => 1,
  });

  /** Header filters plus page and search; null once the user has signed out (nothing is asked). */
  private readonly request = computed(() => {
    const management = this.filters.request();
    return management
      ? { ...management, page: this.page(), pageSize: this.pageSize, search: this.filters.search() }
      : null;
  });

  /** The page answered by the API; the previous one stays on screen until the next arrives. */
  protected readonly items = toSignal(
    toObservable(this.request).pipe(
      switchMap((request) =>
        request
          ? // A failed request shows an empty table and does not stop the next ones.
            this.warehouseService.list(request).pipe(catchError(() => of(NO_ITEMS)))
          : EMPTY,
      ),
    ),
    { initialValue: NO_ITEMS },
  );

  protected readonly columns: readonly TableColumn<WarehouseItem>[] = [
    { field: 'name', header: 'management.warehouse.table.name' },
    { field: 'total', header: 'management.warehouse.table.total', align: 'center' },
    { field: 'available', header: 'management.warehouse.table.available', align: 'center' },
  ];
}
