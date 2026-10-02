import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ManagementFiltersBehaviour } from '../../behaviours/management/management-filters.behaviour';
import { TableErrorPopup } from '../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import { Table, TableColumn } from '../../components/shared/ui/tables/table/table';
import { WarehouseItem } from '../../entities/warehouse/warehouse-item';
import { WarehouseService } from '../../services/api/warehouse/warehouse.service';

/** Group `management.warehouse.table` of the translations: the column headers. */
interface WarehouseHeaders {
  name: string;
  total: string;
  available: string;
}

/**
 * Warehouse of the property: articles with total and available quantity, page by page. The table
 * loads them by itself with the warehouse API and the header filters.
 */
@Component({
  selector: 'app-warehouse',
  imports: [Table, TableErrorPopup, TranslatePipe],
  templateUrl: './warehouse.html',
  styleUrl: './warehouse.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Warehouse {
  protected readonly filters = inject(ManagementFiltersBehaviour);
  protected readonly warehouse = inject(WarehouseService);
  private readonly translateService = inject(TranslateService);

  /** A page of the table could not be loaded: shows the error popup. */
  protected readonly loadFailed = signal(false);
  /** Icons of the table, in its order: [search magnifier, search X, delete, duplicate, edit]. */
  protected readonly tableIcons = [
    '/assets/images/search.svg',
    '/assets/images/clear.svg',
    '/assets/images/delete.svg',
    '/assets/images/duplicate.svg',
    '/assets/images/edit.svg',
  ] as const;
  private readonly headers = toSignal(
    this.translateService.stream('management.warehouse.table') as Observable<WarehouseHeaders>,
    { initialValue: { name: '', total: '', available: '' } },
  );
  /** Columns with translated headers (they follow the language). */
  protected readonly columns = computed<readonly TableColumn<WarehouseItem>[]>(() => [
    { field: 'name', header: this.headers().name, width: 50, sortable: true },
    { field: 'total', header: this.headers().total, align: 'center', sortable: true },
    { field: 'available', header: this.headers().available, align: 'center', sortable: true },
  ]);
}
