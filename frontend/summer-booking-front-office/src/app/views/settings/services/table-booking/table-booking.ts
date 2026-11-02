import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { DeleteItemsPopup } from '../../../../components/management/delete-items-popup/delete-items-popup';
import { DuplicateItemsPopup } from '../../../../components/management/duplicate-items-popup/duplicate-items-popup';
import { standardRowActions } from '../../../../components/shared/tables/standard-row-actions';
import { TableErrorPopup } from '../../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import {
  Table,
  TableColumn,
  TableRowId,
} from '../../../../components/shared/ui/tables/table/table';
import { PageTitle } from '../../../../components/shared/ui/titles/page-title/page-title';
import { TableServiceItem } from '../../../../entities/settings/services/table-service-item';
import { CatalogService } from '../../../../services/api/catalog/catalog.service';

interface TableBookingTexts {
  service_name: string;
}

@Component({
  selector: 'app-table-booking',
  imports: [
    DeleteItemsPopup,
    DuplicateItemsPopup,
    PageTitle,
    Table,
    TableErrorPopup,
    TranslatePipe,
  ],
  templateUrl: './table-booking.html',
  styleUrl: './table-booking.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TableBooking {
  private readonly auth = inject(AuthBehaviour);
  protected readonly catalog = inject(CatalogService);
  private readonly translateService = inject(TranslateService);

  protected readonly loadFailed = signal(false);
  protected readonly idSelectedTableServices = signal<readonly TableRowId[]>([]);
  protected readonly deleting = signal(false);
  protected readonly idTableServicesToDelete = signal<readonly string[]>([]);
  protected readonly tableServiceNameToDelete = signal<string | null>(null);
  protected readonly duplicating = signal(false);
  protected readonly idTableServicesToDuplicate = signal<readonly string[]>([]);
  protected readonly tableServiceNameToDuplicate = signal<string | null>(null);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
  protected readonly titleIcon = ['/assets/images/Services_TableBookingTitle.png'] as const;
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  /** [X, warning]. */
  protected readonly alertIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_Warning.svg',
  ] as const;
  /** In the table's order: [search magnifier, search X, duplicate chosen, delete chosen, create]. */
  protected readonly tableIcons = [
    '/assets/images/GeneralIcon_Search.svg',
    '/assets/images/GeneralIcon_Clear.svg',
    '/assets/images/GeneralIcon_DuplicateSelected.svg',
    '/assets/images/GeneralIcon_DeleteSelected.svg',
    '/assets/images/GeneralIcon_Add.svg',
  ] as const;
  private readonly headers = toSignal(
    this.translateService.stream(
      'management.settings.services.table_booking.table',
    ) as Observable<TableBookingTexts>,
    { initialValue: { service_name: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<TableServiceItem>[]>(() => [
    { field: 'serviceName', header: this.headers().service_name, sortable: true },
  ]);
  protected readonly rowActions = standardRowActions<TableServiceItem>({
    delete: ({ idTableService, serviceName }) =>
      idTableService && this.askDelete([idTableService], serviceName),
    duplicate: ({ idTableService, serviceName }) =>
      idTableService && this.askDuplicate([idTableService], serviceName),
    edit: () => {},
  });

  protected readonly deleteTableServices = (idProperty: string, idTableServices: string[]) =>
    this.catalog.deleteTableServiceItems({ idProperty, idTableServices });
  protected readonly duplicateTableServices = (idProperty: string, idTableServices: string[]) =>
    this.catalog.duplicateTableServiceItems({ idProperty, idTableServices });

  protected askDelete(
    idTableServices: readonly TableRowId[],
    tableServiceName: string | null = null,
  ): void {
    this.idTableServicesToDelete.set(idTableServices.map(String));
    this.tableServiceNameToDelete.set(tableServiceName);
    this.deleting.set(true);
  }

  protected askDuplicate(
    idTableServices: readonly TableRowId[],
    tableServiceName: string | null = null,
  ): void {
    this.idTableServicesToDuplicate.set(idTableServices.map(String));
    this.tableServiceNameToDuplicate.set(tableServiceName);
    this.duplicating.set(true);
  }

  /** Only after "Duplica selezione" (no name): the chosen rows are done with. */
  protected afterDuplicate(): void {
    if (this.tableServiceNameToDuplicate() === null) {
      this.idSelectedTableServices.set([]);
    }
  }

  protected forgetDeleted(idTableServices: readonly string[]): void {
    this.idSelectedTableServices.update((idSelected) =>
      idSelected.filter(
        (idSelectedTableService) => !idTableServices.includes(String(idSelectedTableService)),
      ),
    );
  }
}
