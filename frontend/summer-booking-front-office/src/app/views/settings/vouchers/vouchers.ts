import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, map, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import {
  DATE_FORMATS,
  DateLanguageBehaviour,
} from '../../../behaviours/i18n/date-language.behaviour';
import { DeleteItemsPopup } from '../../../components/management/delete-items-popup/delete-items-popup';
import { DuplicateItemsPopup } from '../../../components/management/duplicate-items-popup/duplicate-items-popup';
import { VoucherAddItemPanel } from '../../../components/management/settings/vouchers/voucher-add-item-panel/voucher-add-item-panel';
import { VoucherEditItemPanel } from '../../../components/management/settings/vouchers/voucher-edit-item-panel/voucher-edit-item-panel';
import { standardRowActions } from '../../../components/shared/tables/standard-row-actions';
import { PageTitle } from '../../../components/shared/ui/titles/page-title/page-title';
import { TableErrorPopup } from '../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import {
  Table,
  TableColumn,
  TableLoad,
  TableRowId,
} from '../../../components/shared/ui/tables/table/table';
import { DiscountType } from '../../../entities/enums/discount-type';
import { VoucherService } from '../../../services/api/vouchers/voucher.service';

interface VouchersTexts {
  table: {
    voucher_name: string;
    valid_from: string;
    valid_to: string;
    discount_type: string;
    discount_value: string;
  };
  discount_type: {
    percentage: string;
    fixed: string;
  };
}

interface VoucherRow {
  idVoucher: string | null;
  voucherName: string | null;
  validFrom: string | null;
  validTo: string | null;
  discountType: string | null;
  discountValue: string | null;
}

const DISCOUNT_TYPE_KEYS: Record<DiscountType, keyof VouchersTexts['discount_type']> = {
  [DiscountType.Percentage]: 'percentage',
  [DiscountType.Fixed]: 'fixed',
};

@Component({
  selector: 'app-vouchers',
  imports: [
    DeleteItemsPopup,
    DuplicateItemsPopup,
    PageTitle,
    Table,
    TableErrorPopup,
    TranslatePipe,
    VoucherAddItemPanel,
    VoucherEditItemPanel,
  ],
  templateUrl: './vouchers.html',
  styleUrl: './vouchers.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Vouchers {
  private readonly auth = inject(AuthBehaviour);
  private readonly vouchers = inject(VoucherService);
  private readonly translateService = inject(TranslateService);
  private readonly dateLanguage = inject(DateLanguageBehaviour);

  protected readonly loadFailed = signal(false);
  protected readonly adding = signal(false);
  protected readonly editing = signal(false);
  protected readonly idVoucherToEdit = signal<string | null>(null);
  protected readonly idSelectedVouchers = signal<readonly TableRowId[]>([]);
  protected readonly deleting = signal(false);
  protected readonly idVouchersToDelete = signal<readonly string[]>([]);
  protected readonly voucherNameToDelete = signal<string | null>(null);
  protected readonly duplicating = signal(false);
  protected readonly idVouchersToDuplicate = signal<readonly string[]>([]);
  protected readonly voucherNameToDuplicate = signal<string | null>(null);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
  protected readonly titleIcon = ['/assets/images/Vouchers_Title.svg'] as const;
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  /** [X, warning]. */
  protected readonly alertIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_Warning.svg',
  ] as const;
  /** In the table's order: [search magnifier, search X, duplicate chosen, delete chosen, create]. */
  protected readonly tableIcons = [
    '',
    '',
    '/assets/images/GeneralIcon_DuplicateSelected.svg',
    '/assets/images/GeneralIcon_DeleteSelected.svg',
    '/assets/images/GeneralIcon_Add.svg',
  ] as const;
  private readonly headers = toSignal(
    this.translateService.stream('management.settings.vouchers.table') as Observable<
      VouchersTexts['table']
    >,
    {
      initialValue: {
        voucher_name: '',
        valid_from: '',
        valid_to: '',
        discount_type: '',
        discount_value: '',
      },
    },
  );
  protected readonly columns = computed<readonly TableColumn<VoucherRow>[]>(() => [
    { field: 'voucherName', header: this.headers().voucher_name, sortable: true },
    { field: 'validFrom', header: this.headers().valid_from, sortable: true },
    { field: 'validTo', header: this.headers().valid_to, sortable: true },
    { field: 'discountType', header: this.headers().discount_type, sortable: true },
    { field: 'discountValue', header: this.headers().discount_value, sortable: true },
  ]);
  protected readonly rowActions = standardRowActions<VoucherRow>({
    delete: ({ idVoucher, voucherName }) => idVoucher && this.askDelete([idVoucher], voucherName),
    duplicate: ({ idVoucher, voucherName }) =>
      idVoucher && this.askDuplicate([idVoucher], voucherName),
    edit: ({ idVoucher }) => idVoucher && this.openEdit(idVoucher),
  });

  protected readonly load: TableLoad<{ idProperty: string }, VoucherRow> = (request) =>
    this.vouchers.voucherList(request).pipe(
      switchMap((page) =>
        (
          this.translateService.get('management.settings.vouchers') as Observable<VouchersTexts>
        ).pipe(
          map((texts) => {
            const locale = this.dateLanguage.locale();
            const day = new Intl.DateTimeFormat(locale, {
              ...DATE_FORMATS.display.dateInput,
              timeZone: 'UTC',
            });
            const percentage = new Intl.NumberFormat(locale, {
              style: 'percent',
              maximumFractionDigits: 2,
            });
            const amount = new Intl.NumberFormat(locale, {
              style: 'currency',
              currency: 'EUR',
              minimumFractionDigits: 0,
              maximumFractionDigits: 2,
            });
            return {
              total: page.total,
              rows: page.rows.map((item): VoucherRow => ({
                ...item,
                validFrom: item.validFrom ? day.format(new Date(item.validFrom)) : null,
                validTo: item.validTo ? day.format(new Date(item.validTo)) : null,
                discountType: item.discountType
                  ? texts.discount_type[DISCOUNT_TYPE_KEYS[item.discountType]]
                  : null,
                discountValue:
                  item.discountValue === null
                    ? null
                    : item.discountType === DiscountType.Percentage
                      ? percentage.format(item.discountValue / 100)
                      : amount.format(item.discountValue),
              })),
            };
          }),
        ),
      ),
    );

  protected readonly deleteVouchers = (idProperty: string, idVouchers: string[]) =>
    this.vouchers.deleteVoucherItems({ idProperty, idVouchers });
  protected readonly duplicateVouchers = (idProperty: string, idVouchers: string[]) =>
    this.vouchers.duplicateVoucherItems({ idProperty, idVouchers });

  protected askDelete(idVouchers: readonly TableRowId[], voucherName: string | null = null): void {
    this.idVouchersToDelete.set(idVouchers.map(String));
    this.voucherNameToDelete.set(voucherName);
    this.deleting.set(true);
  }

  protected askDuplicate(
    idVouchers: readonly TableRowId[],
    voucherName: string | null = null,
  ): void {
    this.idVouchersToDuplicate.set(idVouchers.map(String));
    this.voucherNameToDuplicate.set(voucherName);
    this.duplicating.set(true);
  }

  protected openEdit(idVoucher: string): void {
    this.idVoucherToEdit.set(idVoucher);
    this.editing.set(true);
  }

  /** Only after "Duplica selezione" (no name): the chosen rows are done with. */
  protected afterDuplicate(): void {
    if (this.voucherNameToDuplicate() === null) {
      this.idSelectedVouchers.set([]);
    }
  }

  protected forgetDeleted(idVouchers: readonly string[]): void {
    this.idSelectedVouchers.update((idSelected) =>
      idSelected.filter((idSelectedVoucher) => !idVouchers.includes(String(idSelectedVoucher))),
    );
  }
}
