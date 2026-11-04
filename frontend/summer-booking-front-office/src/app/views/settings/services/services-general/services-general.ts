import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, map } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { DateLanguageBehaviour } from '../../../../behaviours/i18n/date-language.behaviour';
import { DeleteItemsPopup } from '../../../../components/management/delete-items-popup/delete-items-popup';
import { DuplicateItemsPopup } from '../../../../components/management/duplicate-items-popup/duplicate-items-popup';
import { GeneralServiceAddItemPanel } from '../../../../components/management/settings/services/general-service-add-item-panel/general-service-add-item-panel';
import { standardRowActions } from '../../../../components/shared/tables/standard-row-actions';
import { TableErrorPopup } from '../../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import {
  Table,
  TableColumn,
  TableLoad,
  TableRowId,
} from '../../../../components/shared/ui/tables/table/table';
import { PageTitle } from '../../../../components/shared/ui/titles/page-title/page-title';
import { CatalogService } from '../../../../services/api/catalog/catalog.service';

interface ServicesGeneralTexts {
  service: string;
  price: string;
}

interface StandardServiceRow {
  idStandardService: string | null;
  serviceName: string | null;
  price: string | null;
}

@Component({
  selector: 'app-services-general',
  imports: [
    DeleteItemsPopup,
    DuplicateItemsPopup,
    GeneralServiceAddItemPanel,
    PageTitle,
    Table,
    TableErrorPopup,
    TranslatePipe,
  ],
  templateUrl: './services-general.html',
  styleUrl: './services-general.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesGeneral {
  private readonly auth = inject(AuthBehaviour);
  private readonly catalog = inject(CatalogService);
  private readonly translateService = inject(TranslateService);
  private readonly dateLanguage = inject(DateLanguageBehaviour);

  protected readonly loadFailed = signal(false);
  protected readonly adding = signal(false);
  protected readonly idSelectedStandardServices = signal<readonly TableRowId[]>([]);
  protected readonly deleting = signal(false);
  protected readonly idStandardServicesToDelete = signal<readonly string[]>([]);
  protected readonly standardServiceNameToDelete = signal<string | null>(null);
  protected readonly duplicating = signal(false);
  protected readonly idStandardServicesToDuplicate = signal<readonly string[]>([]);
  protected readonly standardServiceNameToDuplicate = signal<string | null>(null);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
  protected readonly titleIcon = ['/assets/images/Services_StandardTitle.png'] as const;
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
      'management.settings.services.standard_services.table',
    ) as Observable<ServicesGeneralTexts>,
    { initialValue: { service: '', price: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<StandardServiceRow>[]>(() => [
    { field: 'serviceName', header: this.headers().service, sortable: true },
    { field: 'price', header: this.headers().price, sortable: true },
  ]);
  protected readonly rowActions = standardRowActions<StandardServiceRow>({
    delete: ({ idStandardService, serviceName }) =>
      idStandardService && this.askDelete([idStandardService], serviceName),
    duplicate: ({ idStandardService, serviceName }) =>
      idStandardService && this.askDuplicate([idStandardService], serviceName),
    edit: () => {},
  });

  protected readonly load: TableLoad<{ idProperty: string }, StandardServiceRow> = (request) =>
    this.catalog.standardServiceList(request).pipe(
      map((page) => {
        const amount = new Intl.NumberFormat(this.dateLanguage.locale(), {
          style: 'currency',
          currency: 'EUR',
        });
        return {
          total: page.total,
          rows: page.rows.map((item): StandardServiceRow => ({
            ...item,
            price: item.price === null ? null : amount.format(item.price),
          })),
        };
      }),
    );

  protected readonly deleteStandardServices = (idProperty: string, idStandardServices: string[]) =>
    this.catalog.deleteStandardServiceItems({ idProperty, idStandardServices });
  protected readonly duplicateStandardServices = (
    idProperty: string,
    idStandardServices: string[],
  ) => this.catalog.duplicateStandardServiceItems({ idProperty, idStandardServices });

  protected askDelete(
    idStandardServices: readonly TableRowId[],
    standardServiceName: string | null = null,
  ): void {
    this.idStandardServicesToDelete.set(idStandardServices.map(String));
    this.standardServiceNameToDelete.set(standardServiceName);
    this.deleting.set(true);
  }

  protected askDuplicate(
    idStandardServices: readonly TableRowId[],
    standardServiceName: string | null = null,
  ): void {
    this.idStandardServicesToDuplicate.set(idStandardServices.map(String));
    this.standardServiceNameToDuplicate.set(standardServiceName);
    this.duplicating.set(true);
  }

  /** Only after "Duplica selezione" (no name): the chosen rows are done with. */
  protected afterDuplicate(): void {
    if (this.standardServiceNameToDuplicate() === null) {
      this.idSelectedStandardServices.set([]);
    }
  }

  protected forgetDeleted(idStandardServices: readonly string[]): void {
    this.idSelectedStandardServices.update((idSelected) =>
      idSelected.filter(
        (idSelectedStandardService) =>
          !idStandardServices.includes(String(idSelectedStandardService)),
      ),
    );
  }
}
