import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { WarehouseService } from '../../../services/api/warehouse/warehouse.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { Table, TableColumn } from '../../../components/shared/ui/tables/table/table';
import { standardRowActions } from '../../../components/shared/tables/standard-row-actions';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';
import { ManagementFiltersBehaviour } from '../../../behaviours/management/management-filters.behaviour';
import { PageTitle } from '../../../components/shared/ui/titles/page-title/page-title';
import { TableErrorPopup } from '../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import { FormWarehouseAddItemPopup } from '../../../components/management/settings/warehouse-setting/form-warehouse-add-item-popup/form-warehouse-add-item-popup';
import { FormWarehouseEditItemPopup } from '../../../components/management/settings/warehouse-setting/form-warehouse-edit-item-popup/form-warehouse-edit-item-popup';

/** Group `management.warehouse.table` of the translations: the column headers used here. */
interface WarehouseSettingsHeaders {
  name: string;
  total: string;
  threshold: string;
}

/**
 * Settings of the warehouse (settings page `warehouse-setting`): the articles with their total and
 * their threshold, page by page, under the title "Impostazioni magazzino".
 */
@Component({
  selector: 'app-warehouse-setting',
  imports: [
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

  /** A page of the table could not be loaded: shows the error popup. */
  protected readonly loadFailed = signal(false);
  /** The create button was pressed: shows the popup to add an article (the table reloads after it). */
  protected readonly adding = signal(false);
  /** The pencil of a row was pressed: shows the popup to change that article. */
  protected readonly editing = signal(false);
  /** The row whose pencil was pressed last. */
  protected readonly editedItem = signal<WarehouseItem | null>(null);
  /** Icon of the page title: the warehouse of the menu, in the main blue. */
  protected readonly titleIcon = ['/assets/images/warehouse-dark.svg'] as const;
  /**
   * Icons of the table, in its order: [search magnifier, search X, duplicate chosen, delete chosen,
   * create]. No title icon: the page has its own title. The icons of the row buttons are in
   * `rowActions`.
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
  /** Columns with translated headers (they follow the language). */
  protected readonly columns = computed<readonly TableColumn<WarehouseItem>[]>(() => [
    { field: 'name', header: this.headers().name, width: 50, sortable: true },
    { field: 'total', header: this.headers().total, align: 'center', sortable: true },
    { field: 'thresholdNumber', header: this.headers().threshold, align: 'center', sortable: true },
  ]);
  /** Buttons of every row: the standard delete, duplicate and edit. */
  protected readonly rowActions = standardRowActions<WarehouseItem>({
    delete: () => this.deleteItem(),
    duplicate: () => this.duplicateItem(),
    edit: (item) => this.editItem(item),
  });

  /** Delete button of a row. */
  private deleteItem(): void {
    // Not connected yet: it will call the delete API of the warehouse.
  }

  /** Duplicate button of a row. */
  private duplicateItem(): void {
    // Not connected yet: it will call the duplicate API of the warehouse.
  }

  /** Edit button of a row: opens the popup to change it, filled with its values. */
  private editItem(item: WarehouseItem): void {
    this.editedItem.set(item);
    this.editing.set(true);
  }
}
