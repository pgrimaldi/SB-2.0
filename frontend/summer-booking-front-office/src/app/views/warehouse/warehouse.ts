import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { AuthBehaviour } from '../../behaviours/auth/auth.behaviour';
import { TableErrorPopup } from '../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import {
  Table,
  TableColumn,
  TableColumnAlign,
} from '../../components/shared/ui/tables/table/table';
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
  private readonly auth = inject(AuthBehaviour);
  protected readonly warehouse = inject(WarehouseService);
  private readonly translateService = inject(TranslateService);

  protected readonly loadFailed = signal(false);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
  protected readonly titleIcon = ['/assets/images/Warehouse_Title.svg'] as const;
  protected readonly closeIcon = ['/assets/images/Warehouse_Close.svg'] as const;
  /**
   * In the table's order: [search magnifier, search X, duplicate chosen, delete chosen, create].
   * No title icon: the page has its own title.
   */
  protected readonly tableIcons = [
    '/assets/images/Warehouse_Search.svg',
    '/assets/images/Warehouse_Clear.svg',
    '/assets/images/Warehouse_DuplicateSelected.svg',
    '/assets/images/Warehouse_DeleteSelected.svg',
    '/assets/images/Warehouse_Add.svg',
  ] as const;

  private readonly headers = toSignal(
    this.translateService.stream('management.warehouse.table') as Observable<WarehouseHeaders>,
    { initialValue: { name: '', total: '', available: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<WarehouseItem>[]>(() => [
    { field: 'name', header: this.headers().name, width: 50, sortable: true },
    {
      field: 'totalQuantity',
      header: this.headers().total,
      align: TableColumnAlign.Center,
      sortable: true,
    },
    {
      field: 'availableQuantity',
      header: this.headers().available,
      align: TableColumnAlign.Center,
      sortable: true,
    },
  ]);
}
