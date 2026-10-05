import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { WarehouseService } from '../../../services/api/warehouse/warehouse.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { Table, TableColumn, TableRowId } from '../../../components/shared/ui/tables/table/table';
import { standardRowActions } from '../../../components/shared/tables/standard-row-actions';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';
import { ManagementFiltersBehaviour } from '../../../behaviours/management/management-filters.behaviour';
import { PageTitle } from '../../../components/shared/ui/titles/page-title/page-title';
import { TableErrorPopup } from '../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import { DeleteWarehouseItemsPopup } from '../../../components/management/settings/warehouse-setting/delete-warehouse-items-popup/delete-warehouse-items-popup';
import { DuplicateWarehouseItemsPopup } from '../../../components/management/settings/warehouse-setting/duplicate-warehouse-items-popup/duplicate-warehouse-items-popup';
import { FormWarehouseAddItemPopup } from '../../../components/management/settings/warehouse-setting/form-warehouse-add-item-popup/form-warehouse-add-item-popup';
import { FormWarehouseEditItemPopup } from '../../../components/management/settings/warehouse-setting/form-warehouse-edit-item-popup/form-warehouse-edit-item-popup';

interface WarehouseSettingsHeaders {
  name: string;
  total: string;
  threshold: string;
}

@Component({
  selector: 'app-warehouse-setting',
  imports: [
    DeleteWarehouseItemsPopup,
    DuplicateWarehouseItemsPopup,
    FormWarehouseAddItemPopup,
    FormWarehouseEditItemPopup,
    PageTitle,
    Table,
    TableErrorPopup,
    TranslatePipe,
  ],
  templateUrl: './warehouse-setting.html',
  styleUrl: './warehouse-setting.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WarehouseSetting {
  protected readonly filters = inject(ManagementFiltersBehaviour);
  protected readonly warehouse = inject(WarehouseService);
  private readonly translateService = inject(TranslateService);

  protected readonly loadFailed = signal(false);
  protected readonly adding = signal(false);
  protected readonly editing = signal(false);
  protected readonly editedItem = signal<WarehouseItem | null>(null);
  protected readonly idSelectedItems = signal<readonly TableRowId[]>([]);
  protected readonly deleting = signal(false);
  protected readonly idItemsToDelete = signal<readonly string[]>([]);
  protected readonly itemNameToDelete = signal<string | null>(null);
  protected readonly duplicating = signal(false);
  protected readonly idItemsToDuplicate = signal<readonly string[]>([]);
  protected readonly itemNameToDuplicate = signal<string | null>(null);
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
    this.translateService.stream(
      'management.warehouse.table',
    ) as Observable<WarehouseSettingsHeaders>,
    { initialValue: { name: '', total: '', threshold: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<WarehouseItem>[]>(() => [
    { field: 'name', header: this.headers().name, width: 50, sortable: true },
    { field: 'totalQuantity', header: this.headers().total, align: 'center', sortable: true },
    {
      field: 'thresholdQuantity',
      header: this.headers().threshold,
      align: 'center',
      sortable: true,
    },
  ]);
  protected readonly rowActions = standardRowActions<WarehouseItem>({
    delete: (item) => this.askDelete([item.idArticle], item.name),
    duplicate: (item) => this.askDuplicate([item.idArticle], item.name),
    edit: (item) => this.editItem(item),
  });

  protected askDelete(idItems: readonly TableRowId[], itemName: string | null = null): void {
    this.idItemsToDelete.set(idItems.map(String));
    this.itemNameToDelete.set(itemName);
    this.deleting.set(true);
  }

  protected askDuplicate(idItems: readonly TableRowId[], itemName: string | null = null): void {
    this.idItemsToDuplicate.set(idItems.map(String));
    this.itemNameToDuplicate.set(itemName);
    this.duplicating.set(true);
  }

  /** Only after "Duplica selezione" (no name): the chosen rows are done with. */
  protected afterDuplicate(): void {
    if (this.itemNameToDuplicate() === null) {
      this.idSelectedItems.set([]);
    }
  }

  protected forgetDeleted(idItems: readonly string[]): void {
    this.idSelectedItems.update((idSelected) =>
      idSelected.filter((idSelectedItem) => !idItems.includes(String(idSelectedItem))),
    );
  }

  private editItem(item: WarehouseItem): void {
    this.editedItem.set(item);
    this.editing.set(true);
  }
}
