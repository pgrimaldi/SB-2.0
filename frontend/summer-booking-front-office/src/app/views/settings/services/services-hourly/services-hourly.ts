import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { DeleteItemsPopup } from '../../../../components/management/delete-items-popup/delete-items-popup';
import { DuplicateItemsPopup } from '../../../../components/management/duplicate-items-popup/duplicate-items-popup';
import { HourlyServiceAddItemPanel } from '../../../../components/management/settings/services/hourly-service-add-item-panel/hourly-service-add-item-panel';
import { standardRowActions } from '../../../../components/shared/tables/standard-row-actions';
import { TableErrorPopup } from '../../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import {
  Table,
  TableColumn,
  TableRowId,
} from '../../../../components/shared/ui/tables/table/table';
import { PageTitle } from '../../../../components/shared/ui/titles/page-title/page-title';
import { HourlyServiceItem } from '../../../../entities/settings/services/hourly-service-item';
import { CatalogService } from '../../../../services/api/catalog/catalog.service';

interface ServicesHourlyTexts {
  service: string;
}

@Component({
  selector: 'app-services-hourly',
  imports: [
    DeleteItemsPopup,
    DuplicateItemsPopup,
    HourlyServiceAddItemPanel,
    PageTitle,
    Table,
    TableErrorPopup,
    TranslatePipe,
  ],
  templateUrl: './services-hourly.html',
  styleUrl: './services-hourly.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesHourly {
  private readonly auth = inject(AuthBehaviour);
  protected readonly catalog = inject(CatalogService);
  private readonly translateService = inject(TranslateService);

  protected readonly loadFailed = signal(false);
  protected readonly adding = signal(false);
  protected readonly idSelectedHourlyServices = signal<readonly TableRowId[]>([]);
  protected readonly deleting = signal(false);
  protected readonly idHourlyServicesToDelete = signal<readonly string[]>([]);
  protected readonly hourlyServiceNameToDelete = signal<string | null>(null);
  protected readonly duplicating = signal(false);
  protected readonly idHourlyServicesToDuplicate = signal<readonly string[]>([]);
  protected readonly hourlyServiceNameToDuplicate = signal<string | null>(null);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
  protected readonly titleIcon = ['/assets/images/Services_HourlyTitle.png'] as const;
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
      'management.settings.services.hourly_services.table',
    ) as Observable<ServicesHourlyTexts>,
    { initialValue: { service: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<HourlyServiceItem>[]>(() => [
    { field: 'serviceName', header: this.headers().service, sortable: true },
  ]);
  protected readonly rowActions = standardRowActions<HourlyServiceItem>({
    delete: ({ idHourlyService, serviceName }) =>
      idHourlyService && this.askDelete([idHourlyService], serviceName),
    duplicate: ({ idHourlyService, serviceName }) =>
      idHourlyService && this.askDuplicate([idHourlyService], serviceName),
    edit: () => {},
  });

  protected readonly deleteHourlyServices = (idProperty: string, idHourlyServices: string[]) =>
    this.catalog.deleteHourlyServiceItems({ idProperty, idHourlyServices });
  protected readonly duplicateHourlyServices = (idProperty: string, idHourlyServices: string[]) =>
    this.catalog.duplicateHourlyServiceItems({ idProperty, idHourlyServices });

  protected askDelete(
    idHourlyServices: readonly TableRowId[],
    hourlyServiceName: string | null = null,
  ): void {
    this.idHourlyServicesToDelete.set(idHourlyServices.map(String));
    this.hourlyServiceNameToDelete.set(hourlyServiceName);
    this.deleting.set(true);
  }

  protected askDuplicate(
    idHourlyServices: readonly TableRowId[],
    hourlyServiceName: string | null = null,
  ): void {
    this.idHourlyServicesToDuplicate.set(idHourlyServices.map(String));
    this.hourlyServiceNameToDuplicate.set(hourlyServiceName);
    this.duplicating.set(true);
  }

  /** Only after "Duplica selezione" (no name): the chosen rows are done with. */
  protected afterDuplicate(): void {
    if (this.hourlyServiceNameToDuplicate() === null) {
      this.idSelectedHourlyServices.set([]);
    }
  }

  protected forgetDeleted(idHourlyServices: readonly string[]): void {
    this.idSelectedHourlyServices.update((idSelected) =>
      idSelected.filter(
        (idSelectedHourlyService) => !idHourlyServices.includes(String(idSelectedHourlyService)),
      ),
    );
  }
}
