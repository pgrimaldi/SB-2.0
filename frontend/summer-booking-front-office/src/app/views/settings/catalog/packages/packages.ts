import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { standardRowActions } from '../../../../components/shared/tables/standard-row-actions';
import { TableErrorPopup } from '../../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import {
  Table,
  TableColumn,
  TableLoad,
  TableRowId,
} from '../../../../components/shared/ui/tables/table/table';
import { PageTitle } from '../../../../components/shared/ui/titles/page-title/page-title';
import { PackageItem } from '../../../../entities/settings/catalog/package-item';
import { PriceListService } from '../../../../services/api/catalog/price-list.service';

interface PackagesTableTexts {
  package_name: string;
}

@Component({
  selector: 'app-packages',
  imports: [PageTitle, Table, TableErrorPopup, TranslatePipe],
  templateUrl: './packages.html',
  styleUrl: './packages.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Packages {
  private readonly auth = inject(AuthBehaviour);
  private readonly priceLists = inject(PriceListService);
  private readonly translateService = inject(TranslateService);

  protected readonly loadFailed = signal(false);
  protected readonly idSelectedPackages = signal<readonly TableRowId[]>([]);
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
      'management.settings.catalog.packages.table',
    ) as Observable<PackagesTableTexts>,
    { initialValue: { package_name: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<PackageItem>[]>(() => [
    { field: 'packageName', header: this.headers().package_name, sortable: true },
  ]);
  protected readonly rowActions = standardRowActions<PackageItem>({
    delete: () => undefined,
    duplicate: () => undefined,
    edit: () => undefined,
  });

  protected readonly load: TableLoad<{ idProperty: string }, PackageItem> = (request) =>
    this.priceLists.packageList(request);
}
