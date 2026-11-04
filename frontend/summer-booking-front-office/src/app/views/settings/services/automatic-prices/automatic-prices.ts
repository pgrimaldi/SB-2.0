import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, map } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { DateLanguageBehaviour } from '../../../../behaviours/i18n/date-language.behaviour';
import { DeleteItemsPopup } from '../../../../components/management/delete-items-popup/delete-items-popup';
import { DuplicateItemsPopup } from '../../../../components/management/duplicate-items-popup/duplicate-items-popup';
import { AutomaticPriceAddItemPanel } from '../../../../components/management/settings/services/automatic-price-add-item-panel/automatic-price-add-item-panel';
import { AutomaticPriceEditItemPanel } from '../../../../components/management/settings/services/automatic-price-edit-item-panel/automatic-price-edit-item-panel';
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

interface AutomaticPricesTexts {
  service: string;
  price: string;
}

interface AutomaticPriceRow {
  idAutomaticPrice: string | null;
  serviceName: string | null;
  price: string | null;
}

@Component({
  selector: 'app-automatic-prices',
  imports: [
    DeleteItemsPopup,
    AutomaticPriceAddItemPanel,
    AutomaticPriceEditItemPanel,
    DuplicateItemsPopup,
    PageTitle,
    Table,
    TableErrorPopup,
    TranslatePipe,
  ],
  templateUrl: './automatic-prices.html',
  styleUrl: './automatic-prices.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AutomaticPrices {
  private readonly auth = inject(AuthBehaviour);
  private readonly catalog = inject(CatalogService);
  private readonly translateService = inject(TranslateService);
  private readonly dateLanguage = inject(DateLanguageBehaviour);

  protected readonly loadFailed = signal(false);
  protected readonly adding = signal(false);
  protected readonly editing = signal(false);
  protected readonly idAutomaticPriceToEdit = signal<string | null>(null);
  protected readonly idSelectedAutomaticPrices = signal<readonly TableRowId[]>([]);
  protected readonly deleting = signal(false);
  protected readonly idAutomaticPricesToDelete = signal<readonly string[]>([]);
  protected readonly automaticPriceNameToDelete = signal<string | null>(null);
  protected readonly duplicating = signal(false);
  protected readonly idAutomaticPricesToDuplicate = signal<readonly string[]>([]);
  protected readonly automaticPriceNameToDuplicate = signal<string | null>(null);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
  protected readonly titleIcon = ['/assets/images/Services_AutomaticPricesTitle.png'] as const;
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
      'management.settings.services.automatic_prices.table',
    ) as Observable<AutomaticPricesTexts>,
    { initialValue: { service: '', price: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<AutomaticPriceRow>[]>(() => [
    { field: 'serviceName', header: this.headers().service, sortable: true },
    { field: 'price', header: this.headers().price, sortable: true },
  ]);
  protected readonly rowActions = standardRowActions<AutomaticPriceRow>({
    delete: ({ idAutomaticPrice, serviceName }) =>
      idAutomaticPrice && this.askDelete([idAutomaticPrice], serviceName),
    duplicate: ({ idAutomaticPrice, serviceName }) =>
      idAutomaticPrice && this.askDuplicate([idAutomaticPrice], serviceName),
    edit: ({ idAutomaticPrice }) => idAutomaticPrice && this.openEdit(idAutomaticPrice),
  });

  protected readonly load: TableLoad<{ idProperty: string }, AutomaticPriceRow> = (request) =>
    this.catalog.automaticPriceList(request).pipe(
      map((page) => {
        const amount = new Intl.NumberFormat(this.dateLanguage.locale(), {
          style: 'currency',
          currency: 'EUR',
        });
        return {
          total: page.total,
          rows: page.rows.map((item): AutomaticPriceRow => ({
            ...item,
            price: item.price === null ? null : amount.format(item.price),
          })),
        };
      }),
    );

  protected readonly deleteAutomaticPrices = (idProperty: string, idAutomaticPrices: string[]) =>
    this.catalog.deleteAutomaticPriceItems({ idProperty, idAutomaticPrices });
  protected readonly duplicateAutomaticPrices = (idProperty: string, idAutomaticPrices: string[]) =>
    this.catalog.duplicateAutomaticPriceItems({ idProperty, idAutomaticPrices });

  protected openEdit(idAutomaticPrice: string): void {
    this.idAutomaticPriceToEdit.set(idAutomaticPrice);
    this.editing.set(true);
  }

  protected askDelete(
    idAutomaticPrices: readonly TableRowId[],
    automaticPriceName: string | null = null,
  ): void {
    this.idAutomaticPricesToDelete.set(idAutomaticPrices.map(String));
    this.automaticPriceNameToDelete.set(automaticPriceName);
    this.deleting.set(true);
  }

  protected askDuplicate(
    idAutomaticPrices: readonly TableRowId[],
    automaticPriceName: string | null = null,
  ): void {
    this.idAutomaticPricesToDuplicate.set(idAutomaticPrices.map(String));
    this.automaticPriceNameToDuplicate.set(automaticPriceName);
    this.duplicating.set(true);
  }

  /** Only after "Duplica selezione" (no name): the chosen rows are done with. */
  protected afterDuplicate(): void {
    if (this.automaticPriceNameToDuplicate() === null) {
      this.idSelectedAutomaticPrices.set([]);
    }
  }

  protected forgetDeleted(idAutomaticPrices: readonly string[]): void {
    this.idSelectedAutomaticPrices.update((idSelected) =>
      idSelected.filter(
        (idSelectedAutomaticPrice) => !idAutomaticPrices.includes(String(idSelectedAutomaticPrice)),
      ),
    );
  }
}
