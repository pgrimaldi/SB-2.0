import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ManagementFiltersBehaviour } from '../../behaviours/management/management-filters.behaviour';
import { Table, TableColumn } from '../../components/shared/ui/tables/table/table';
import { WarehouseItem } from '../../entities/warehouse/warehouse-item';
import { WarehouseService } from '../../services/api/warehouse/warehouse.service';

/**
 * Warehouse of the property: articles with total and available quantity, page by page. The table
 * loads them by itself with the warehouse API and the header filters.
 */
@Component({
  selector: 'app-warehouse',
  imports: [Table, TranslatePipe],
  templateUrl: './warehouse.html',
  styleUrl: './warehouse.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Warehouse {
  protected readonly filters = inject(ManagementFiltersBehaviour);
  protected readonly warehouse = inject(WarehouseService);

  private readonly headers = toSignal(
    inject(TranslateService).stream('management.warehouse.table') as Observable<
      Record<string, string>
    >,
    { initialValue: {} as Record<string, string> },
  );
  /** Columns with translated headers (they follow the language). */
  protected readonly columns = computed<readonly TableColumn<WarehouseItem>[]>(() => [
    { field: 'name', header: this.headers()['name'], width: 50, sortable: true },
    { field: 'total', header: this.headers()['total'], align: 'center', sortable: true },
    { field: 'available', header: this.headers()['available'], align: 'center', sortable: true },
  ]);
}
