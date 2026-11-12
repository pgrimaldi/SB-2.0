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
  TableLoad,
  TableRowId,
} from '../../../../components/shared/ui/tables/table/table';
import { PageTitle } from '../../../../components/shared/ui/titles/page-title/page-title';
import { AdditionalItem } from '../../../../entities/settings/catalog/additional-item';
import { PriceListService } from '../../../../services/api/catalog/price-list.service';

interface AdditionalItemsTableTexts {
  additional_item_name: string;
}

@Component({
  selector: 'app-additional-items',
  imports: [
    DeleteItemsPopup,
    DuplicateItemsPopup,
    PageTitle,
    Table,
    TableErrorPopup,
    TranslatePipe,
  ],
  templateUrl: './additional-items.html',
  styleUrl: './additional-items.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdditionalItems {
  private readonly auth = inject(AuthBehaviour);
  private readonly priceLists = inject(PriceListService);
  private readonly translateService = inject(TranslateService);

  protected readonly loadFailed = signal(false);
  protected readonly idSelectedAdditionalItems = signal<readonly TableRowId[]>([]);
  protected readonly deleting = signal(false);
  protected readonly idAdditionalItemsToDelete = signal<readonly string[]>([]);
  protected readonly additionalItemNameToDelete = signal<string | null>(null);
  protected readonly duplicating = signal(false);
  protected readonly idAdditionalItemsToDuplicate = signal<readonly string[]>([]);
  protected readonly additionalItemNameToDuplicate = signal<string | null>(null);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
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
      'management.settings.catalog.additional_items.table',
    ) as Observable<AdditionalItemsTableTexts>,
    { initialValue: { additional_item_name: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<AdditionalItem>[]>(() => [
    { field: 'additionalItemName', header: this.headers().additional_item_name, sortable: true },
  ]);
  protected readonly rowActions = standardRowActions<AdditionalItem>({
    delete: ({ idAdditionalItem, additionalItemName }) =>
      idAdditionalItem && this.askDelete([idAdditionalItem], additionalItemName),
    duplicate: ({ idAdditionalItem, additionalItemName }) =>
      idAdditionalItem && this.askDuplicate([idAdditionalItem], additionalItemName),
    edit: () => undefined,
  });

  protected readonly load: TableLoad<{ idProperty: string }, AdditionalItem> = (request) =>
    this.priceLists.additionalItemList(request);

  protected readonly deleteAdditionalItems = (idProperty: string, idAdditionalItems: string[]) =>
    this.priceLists.deleteAdditionalItems({ idProperty, idAdditionalItems });
  protected readonly duplicateAdditionalItems = (idProperty: string, idAdditionalItems: string[]) =>
    this.priceLists.duplicateAdditionalItems({ idProperty, idAdditionalItems });

  protected askDelete(
    idAdditionalItems: readonly TableRowId[],
    additionalItemName: string | null = null,
  ): void {
    this.idAdditionalItemsToDelete.set(idAdditionalItems.map(String));
    this.additionalItemNameToDelete.set(additionalItemName);
    this.deleting.set(true);
  }

  protected askDuplicate(
    idAdditionalItems: readonly TableRowId[],
    additionalItemName: string | null = null,
  ): void {
    this.idAdditionalItemsToDuplicate.set(idAdditionalItems.map(String));
    this.additionalItemNameToDuplicate.set(additionalItemName);
    this.duplicating.set(true);
  }

  /** Only after "Duplica selezione" (no name): the chosen rows are done with. */
  protected afterDuplicate(): void {
    if (this.additionalItemNameToDuplicate() === null) {
      this.idSelectedAdditionalItems.set([]);
    }
  }

  protected forgetDeleted(idAdditionalItems: readonly string[]): void {
    this.idSelectedAdditionalItems.update((idSelected) =>
      idSelected.filter(
        (idSelectedAdditionalItem) => !idAdditionalItems.includes(String(idSelectedAdditionalItem)),
      ),
    );
  }
}
