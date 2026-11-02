import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, map } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { DateLanguageBehaviour } from '../../../../behaviours/i18n/date-language.behaviour';
import { DeleteItemsPopup } from '../../../../components/management/delete-items-popup/delete-items-popup';
import { DuplicateItemsPopup } from '../../../../components/management/duplicate-items-popup/duplicate-items-popup';
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

interface ServicesBarTexts {
  service: string;
  price: string;
}

interface BarServiceRow {
  idBarService: string | null;
  serviceName: string | null;
  price: string | null;
}

@Component({
  selector: 'app-services-bar',
  imports: [
    DeleteItemsPopup,
    DuplicateItemsPopup,
    PageTitle,
    Table,
    TableErrorPopup,
    TranslatePipe,
  ],
  templateUrl: './services-bar.html',
  styleUrl: './services-bar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesBar {
  private readonly auth = inject(AuthBehaviour);
  private readonly catalog = inject(CatalogService);
  private readonly translateService = inject(TranslateService);
  private readonly dateLanguage = inject(DateLanguageBehaviour);

  protected readonly loadFailed = signal(false);
  protected readonly idSelectedBarServices = signal<readonly TableRowId[]>([]);
  protected readonly deleting = signal(false);
  protected readonly idBarServicesToDelete = signal<readonly string[]>([]);
  protected readonly barServiceNameToDelete = signal<string | null>(null);
  protected readonly duplicating = signal(false);
  protected readonly idBarServicesToDuplicate = signal<readonly string[]>([]);
  protected readonly barServiceNameToDuplicate = signal<string | null>(null);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
  protected readonly titleIcon = ['/assets/images/Services_BarTitle.png'] as const;
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
      'management.settings.services.bar_services.table',
    ) as Observable<ServicesBarTexts>,
    { initialValue: { service: '', price: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<BarServiceRow>[]>(() => [
    { field: 'serviceName', header: this.headers().service, sortable: true },
    { field: 'price', header: this.headers().price, sortable: true },
  ]);
  protected readonly rowActions = standardRowActions<BarServiceRow>({
    delete: ({ idBarService, serviceName }) =>
      idBarService && this.askDelete([idBarService], serviceName),
    duplicate: ({ idBarService, serviceName }) =>
      idBarService && this.askDuplicate([idBarService], serviceName),
    edit: () => {},
  });

  protected readonly load: TableLoad<{ idProperty: string }, BarServiceRow> = (request) =>
    this.catalog.barServiceList(request).pipe(
      map((page) => {
        const amount = new Intl.NumberFormat(this.dateLanguage.locale(), {
          style: 'currency',
          currency: 'EUR',
        });
        return {
          total: page.total,
          rows: page.rows.map((item): BarServiceRow => ({
            ...item,
            price: item.price === null ? null : amount.format(item.price),
          })),
        };
      }),
    );

  protected readonly deleteBarServices = (idProperty: string, idBarServices: string[]) =>
    this.catalog.deleteBarServiceItems({ idProperty, idBarServices });
  protected readonly duplicateBarServices = (idProperty: string, idBarServices: string[]) =>
    this.catalog.duplicateBarServiceItems({ idProperty, idBarServices });

  protected askDelete(
    idBarServices: readonly TableRowId[],
    barServiceName: string | null = null,
  ): void {
    this.idBarServicesToDelete.set(idBarServices.map(String));
    this.barServiceNameToDelete.set(barServiceName);
    this.deleting.set(true);
  }

  protected askDuplicate(
    idBarServices: readonly TableRowId[],
    barServiceName: string | null = null,
  ): void {
    this.idBarServicesToDuplicate.set(idBarServices.map(String));
    this.barServiceNameToDuplicate.set(barServiceName);
    this.duplicating.set(true);
  }

  /** Only after "Duplica selezione" (no name): the chosen rows are done with. */
  protected afterDuplicate(): void {
    if (this.barServiceNameToDuplicate() === null) {
      this.idSelectedBarServices.set([]);
    }
  }

  protected forgetDeleted(idBarServices: readonly string[]): void {
    this.idSelectedBarServices.update((idSelected) =>
      idSelected.filter(
        (idSelectedBarService) => !idBarServices.includes(String(idSelectedBarService)),
      ),
    );
  }
}
