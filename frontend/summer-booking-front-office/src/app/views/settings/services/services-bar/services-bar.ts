import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, map } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { DateLanguageBehaviour } from '../../../../behaviours/i18n/date-language.behaviour';
import { standardRowActions } from '../../../../components/shared/tables/standard-row-actions';
import { TableErrorPopup } from '../../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import { Table, TableColumn, TableLoad } from '../../../../components/shared/ui/tables/table/table';
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
  imports: [PageTitle, Table, TableErrorPopup, TranslatePipe],
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
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
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
    delete: () => {},
    duplicate: () => {},
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
}
