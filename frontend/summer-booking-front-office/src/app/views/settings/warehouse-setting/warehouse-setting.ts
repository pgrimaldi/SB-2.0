import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { WarehouseService } from '../../../services/api/warehouse/warehouse.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import {
  Table,
  TableColumn,
  TableColumnAlign,
  TableRowId,
} from '../../../components/shared/ui/tables/table/table';
import { standardRowActions } from '../../../components/shared/tables/standard-row-actions';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { PageTitle } from '../../../components/shared/ui/titles/page-title/page-title';
import { TableErrorPopup } from '../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import { DeleteItemsPopup } from '../../../components/management/delete-items-popup/delete-items-popup';
import { DuplicateItemsPopup } from '../../../components/management/duplicate-items-popup/duplicate-items-popup';
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
    DeleteItemsPopup,
    DuplicateItemsPopup,
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
  private readonly auth = inject(AuthBehaviour);
  protected readonly warehouse = inject(WarehouseService);
  private readonly translateService = inject(TranslateService);

  protected readonly loadFailed = signal(false);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
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
  protected readonly titleIcon = ['/assets/images/GeneralIcon_Warehouse.svg'] as const;
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  /** [X, warning]. */
  protected readonly alertIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_Warning.svg',
  ] as const;
  /**
   * In the table's order: [search magnifier, search X, duplicate chosen, delete chosen, create].
   * No title icon: the page has its own title. The row button icons come from `rowActions`.
   */
  protected readonly tableIcons = [
    '/assets/images/GeneralIcon_Search.svg',
    '/assets/images/GeneralIcon_Clear.svg',
    '/assets/images/GeneralIcon_DuplicateSelected.svg',
    '/assets/images/GeneralIcon_DeleteSelected.svg',
    '/assets/images/GeneralIcon_Add.svg',
  ] as const;
  private readonly headers = toSignal(
    this.translateService.stream(
      'management.warehouse.table',
    ) as Observable<WarehouseSettingsHeaders>,
    { initialValue: { name: '', total: '', threshold: '' } },
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
      field: 'thresholdQuantity',
      header: this.headers().threshold,
      align: TableColumnAlign.Center,
      sortable: true,
    },
  ]);
  protected readonly rowActions = standardRowActions<WarehouseItem>({
    delete: ({ idArticle, name }) => idArticle && this.askDelete([idArticle], name),
    duplicate: ({ idArticle, name }) => idArticle && this.askDuplicate([idArticle], name),
    edit: (item) => this.editItem(item),
  });

  protected readonly deleteItems = (idProperty: string, idItems: string[]) =>
    this.warehouse.deleteWarehouseItems({ idProperty, idItems });
  protected readonly duplicateItems = (idProperty: string, idItems: string[]) =>
    this.warehouse.duplicateWarehouseItems({ idProperty, idItems });

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
