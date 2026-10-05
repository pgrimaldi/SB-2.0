import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ManagementFiltersBehaviour } from '../../behaviours/management/management-filters.behaviour';
import { TableErrorPopup } from '../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import { Table, TableColumn } from '../../components/shared/ui/tables/table/table';
import { standardRowActions } from '../../components/shared/tables/standard-row-actions';
import { PageTitle } from '../../components/shared/ui/titles/page-title/page-title';
import { WarehouseItem } from '../../entities/warehouse/warehouse-item';
import { WarehouseService } from '../../services/api/warehouse/warehouse.service';

interface WarehouseHeaders {
  name: string;
  total: string;
  available: string;
}

@Component({
  selector: 'app-warehouse',
  imports: [PageTitle, Table, TableErrorPopup, TranslatePipe],
  templateUrl: './warehouse.html',
  styleUrl: './warehouse.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Warehouse {
  protected readonly filters = inject(ManagementFiltersBehaviour);
  protected readonly warehouse = inject(WarehouseService);
  private readonly translateService = inject(TranslateService);

  protected readonly loadFailed = signal(false);
  protected readonly titleIcon = ['/assets/images/warehouse-dark.svg'] as const;
  /**
   * In the table's order: [search magnifier, search X, duplicate chosen, delete chosen, create].
   * No title icon: the page has its own title. The row button icons come from `rowActions`.
   */
  protected readonly tableIcons = [
    '/assets/images/search.svg',
    '/assets/images/clear.svg',
    '/assets/images/duplicate-selected.svg',
    '/assets/images/delete-selected.svg',
    '/assets/images/add.svg',
  ] as const;

  private readonly headers = toSignal(
    this.translateService.stream('management.warehouse.table') as Observable<WarehouseHeaders>,
    { initialValue: { name: '', total: '', available: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<WarehouseItem>[]>(() => [
    { field: 'name', header: this.headers().name, width: 50, sortable: true },
    { field: 'total', header: this.headers().total, align: 'center', sortable: true },
    { field: 'available', header: this.headers().available, align: 'center', sortable: true },
  ]);
  protected readonly rowActions = standardRowActions<WarehouseItem>({
    delete: () => this.deleteItem(),
    duplicate: () => this.duplicateItem(),
    edit: () => this.editItem(),
  });

  private deleteItem(): void {
    // Not connected yet: it will call the delete API of the warehouse.
  }

  private duplicateItem(): void {
    // Not connected yet: it will call the duplicate API of the warehouse.
  }

  private editItem(): void {
    // Not connected yet: it will open the edit form of the article.
  }
}
