import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { WarehouseService } from '../../../services/api/warehouse/warehouse.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../entities/errors/api-problem';
import {
  Table,
  TableColumn,
  TableColumnAlign,
  TableRowId,
} from '../../../components/shared/ui/tables/table/table';
import { standardRowActions } from '../../../components/shared/tables/standard-row-actions';
import { WarehouseSettingItem } from '../../../entities/warehouse/warehouse-setting-item';
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
  protected readonly params = computed<object | null>(() =>
    this.auth.user()?.propertyId ? {} : null,
  );
  protected readonly adding = signal(false);
  protected readonly editing = signal(false);
  protected readonly editedItem = signal<WarehouseSettingItem | null>(null);
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
  protected readonly columns = computed<readonly TableColumn<WarehouseSettingItem>[]>(() => [
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
  protected readonly rowActions = standardRowActions<WarehouseSettingItem>({
    delete: ({ idArticle, name }) => idArticle && this.askDelete([idArticle], name),
    duplicate: ({ idArticle, name }) => idArticle && this.askDuplicate([idArticle], name),
    edit: (item) => this.editItem(item),
  });

  protected readonly deleteItems = (idItems: string[]) =>
    this.warehouse
      .deleteArticles(idItems)
      .pipe(catchError((error: unknown) => throwError(() => withUsedIn(error))));
  protected readonly duplicateItems = (idItems: string[]) =>
    this.warehouse.duplicateWarehouseItems(idItems);

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

  /**
   * An article deleted meanwhile by someone else (delete or duplicate): the table shows it is gone
   * and the ids of the action leave the selection (all of them after an action on the selection,
   * where nothing was done).
   */
  protected reloadIfGone(
    problem: ApiProblem,
    table: { reload(): void },
    idItems: readonly string[],
  ): void {
    if (problem.code === ApiErrorCode.InventoryItemNotFound) {
      this.forgetDeleted(idItems);
      table.reload();
    }
  }

  private editItem(item: WarehouseSettingItem): void {
    this.editedItem.set(item);
    this.editing.set(true);
  }
}

/** `inventory.item_in_use` with `usedIn`: the price lists and the agreement price lists together. */
function withUsedIn(error: unknown): unknown {
  if (!(error instanceof HttpErrorResponse)) {
    return error;
  }
  const body: unknown = error.error;
  if (
    typeof body !== 'object' ||
    body === null ||
    !('code' in body) ||
    body.code !== ApiErrorCode.InventoryItemInUse
  ) {
    return error;
  }
  const args: Record<string, unknown> =
    'args' in body && typeof body.args === 'object' && body.args !== null
      ? { ...(body.args as Record<string, unknown>) }
      : {};
  const usedIn = [args['priceLists'], args['agreementPriceLists']].flatMap((names) =>
    Array.isArray(names) ? names : [],
  );
  return new HttpErrorResponse({
    error: { ...body, args: { ...args, usedIn } },
    headers: error.headers,
    status: error.status,
    statusText: error.statusText,
    url: error.url ?? undefined,
  });
}
