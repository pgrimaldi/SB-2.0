import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, map, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import {
  DATE_FORMATS,
  DateLanguageBehaviour,
} from '../../../behaviours/i18n/date-language.behaviour';
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
  imports: [PageTitle, Table, TableErrorPopup, TranslatePipe],
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
  protected readonly idSelectedVouchers = signal<readonly TableRowId[]>([]);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.idProperty;
    return idProperty ? { idProperty } : null;
  });
  protected readonly titleIcon = ['/assets/images/Vouchers_Title.svg'] as const;
  /** In the table's order: [search magnifier, search X, duplicate chosen, delete chosen, create]. */
  protected readonly tableIcons = [
    '',
    '',
    '/assets/images/Vouchers_DuplicateSelected.svg',
    '/assets/images/Vouchers_DeleteSelected.svg',
    '/assets/images/Vouchers_Add.svg',
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
    delete: () => {},
    duplicate: () => {},
    edit: () => {},
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
}
