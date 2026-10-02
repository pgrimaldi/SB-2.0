import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { WarehouseService } from '../../../services/api/warehouse/warehouse.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { Table, TableColumn } from '../../../components/shared/ui/tables/table/table';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';
import { ManagementFiltersBehaviour } from '../../../behaviours/management/management-filters.behaviour';
import { PageTitle } from '../../../components/shared/ui/titles/page-title/page-title';
import { TableErrorPopup } from '../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import { FormWarehouseItemPopup } from '../../../components/management/settings/warehouse-setting/form-warehouse-item-popup/form-warehouse-item-popup';

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
  imports: [FormWarehouseItemPopup, PageTitle, Table, TableErrorPopup, TranslatePipe],
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
  /** The create button was pressed: shows the popup to add an article. */
  protected readonly adding = signal(false);
  /** Icon of the page title: the warehouse of the menu, in the main blue. */
  protected readonly titleIcon = ['/assets/images/warehouse-dark.svg'] as const;
  /**
   * Icons of the table, in its order: [search magnifier, search X, delete, duplicate, edit,
   * duplicate chosen, delete chosen, create]. No title icon: the page has its own title.
   */
  protected readonly tableIcons = [
    '/assets/images/search.svg',
    '/assets/images/clear.svg',
    '/assets/images/delete.svg',
    '/assets/images/duplicate.svg',
    '/assets/images/edit.svg',
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
}
