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
import { PackageItem } from '../../../../entities/settings/catalog/package-item';
import { PriceListService } from '../../../../services/api/catalog/price-list.service';

interface PackagesTableTexts {
  package_name: string;
}

@Component({
  selector: 'app-packages',
  imports: [
    DeleteItemsPopup,
    DuplicateItemsPopup,
    PageTitle,
    Table,
    TableErrorPopup,
    TranslatePipe,
  ],
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
  protected readonly deleting = signal(false);
  protected readonly idPackagesToDelete = signal<readonly string[]>([]);
  protected readonly packageNameToDelete = signal<string | null>(null);
  protected readonly duplicating = signal(false);
  protected readonly idPackagesToDuplicate = signal<readonly string[]>([]);
  protected readonly packageNameToDuplicate = signal<string | null>(null);
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
      'management.settings.catalog.packages.table',
    ) as Observable<PackagesTableTexts>,
    { initialValue: { package_name: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<PackageItem>[]>(() => [
    { field: 'packageName', header: this.headers().package_name, sortable: true },
  ]);
  protected readonly rowActions = standardRowActions<PackageItem>({
    delete: ({ idPackage, packageName }) => idPackage && this.askDelete([idPackage], packageName),
    duplicate: ({ idPackage, packageName }) =>
      idPackage && this.askDuplicate([idPackage], packageName),
    edit: () => undefined,
  });

  protected readonly load: TableLoad<{ idProperty: string }, PackageItem> = (request) =>
    this.priceLists.packageList(request);

  protected readonly deletePackages = (idProperty: string, idPackages: string[]) =>
    this.priceLists.deletePackageItems({ idProperty, idPackages });
  protected readonly duplicatePackages = (idProperty: string, idPackages: string[]) =>
    this.priceLists.duplicatePackageItems({ idProperty, idPackages });

  protected askDelete(idPackages: readonly TableRowId[], packageName: string | null = null): void {
    this.idPackagesToDelete.set(idPackages.map(String));
    this.packageNameToDelete.set(packageName);
    this.deleting.set(true);
  }

  protected askDuplicate(
    idPackages: readonly TableRowId[],
    packageName: string | null = null,
  ): void {
    this.idPackagesToDuplicate.set(idPackages.map(String));
    this.packageNameToDuplicate.set(packageName);
    this.duplicating.set(true);
  }

  /** Only after "Duplica selezione" (no name): the chosen rows are done with. */
  protected afterDuplicate(): void {
    if (this.packageNameToDuplicate() === null) {
      this.idSelectedPackages.set([]);
    }
  }

  protected forgetDeleted(idPackages: readonly string[]): void {
    this.idSelectedPackages.update((idSelected) =>
      idSelected.filter((idSelectedPackage) => !idPackages.includes(String(idSelectedPackage))),
    );
  }
}
