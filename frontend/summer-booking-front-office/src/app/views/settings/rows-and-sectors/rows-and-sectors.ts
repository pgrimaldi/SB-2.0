import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, catchError, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { DeleteItemsPopup } from '../../../components/management/delete-items-popup/delete-items-popup';
import { SectorAddPopup } from '../../../components/management/settings/rows-and-sectors/sector-add-popup/sector-add-popup';
import { SectorEditPopup } from '../../../components/management/settings/rows-and-sectors/sector-edit-popup/sector-edit-popup';
import { DuplicateItemsPopup } from '../../../components/management/duplicate-items-popup/duplicate-items-popup';
import { standardRowActions } from '../../../components/shared/tables/standard-row-actions';
import { TableErrorPopup } from '../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import {
  Table,
  TableColumn,
  TableLoad,
  TableRowId,
} from '../../../components/shared/ui/tables/table/table';
import { PageTitle } from '../../../components/shared/ui/titles/page-title/page-title';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { SectorItem } from '../../../entities/settings/rows-and-sectors/sector-item';
import { SectorService } from '../../../services/api/catalog/sector.service';

interface RowsAndSectorsTableTexts {
  sector_name: string;
}

@Component({
  selector: 'app-rows-and-sectors',
  imports: [
    DeleteItemsPopup,
    DuplicateItemsPopup,
    PageTitle,
    SectorAddPopup,
    SectorEditPopup,
    Table,
    TableErrorPopup,
    TranslatePipe,
  ],
  templateUrl: './rows-and-sectors.html',
  styleUrl: './rows-and-sectors.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RowsAndSectors {
  private readonly auth = inject(AuthBehaviour);
  private readonly sectors = inject(SectorService);
  private readonly translateService = inject(TranslateService);

  protected readonly loadFailed = signal(false);
  protected readonly adding = signal(false);
  protected readonly editing = signal(false);
  protected readonly sectorToEdit = signal<SectorItem | null>(null);
  protected readonly idSelectedSectors = signal<readonly TableRowId[]>([]);
  protected readonly deleting = signal(false);
  protected readonly idSectorsToDelete = signal<readonly string[]>([]);
  protected readonly sectorNameToDelete = signal<string | null>(null);
  protected readonly duplicating = signal(false);
  protected readonly idSectorsToDuplicate = signal<readonly string[]>([]);
  protected readonly sectorNameToDuplicate = signal<string | null>(null);
  protected readonly params = computed<object | null>(() =>
    this.auth.user()?.propertyId ? {} : null,
  );
  protected readonly titleIcon = ['/assets/images/GeneralIcon_CalendarFilled.svg'] as const;
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
      'management.settings.rows_and_sectors.table',
    ) as Observable<RowsAndSectorsTableTexts>,
    { initialValue: { sector_name: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<SectorItem>[]>(() => [
    { field: 'sectorName', header: this.headers().sector_name, sortable: true },
  ]);
  protected readonly rowActions = standardRowActions<SectorItem>({
    delete: ({ idSector, sectorName }) => idSector && this.askDelete([idSector], sectorName),
    duplicate: ({ idSector, sectorName }) => idSector && this.askDuplicate([idSector], sectorName),
    edit: (sector) => this.openEdit(sector),
  });

  protected readonly load: TableLoad<object, SectorItem> = (request) =>
    this.sectors.sectorList(request);

  protected readonly deleteSectors = (idSectors: string[]) => {
    const sectorName = this.sectorNameToDelete();
    return sectorName === null
      ? this.sectors.deleteSectors(idSectors)
      : this.sectors
          .deleteSector(idSectors[0])
          .pipe(
            catchError((error: unknown) => throwError(() => withSectorName(error, sectorName))),
          );
  };
  protected readonly duplicateSectors = (idSectors: string[]) =>
    this.sectors.duplicateSectors(idSectors);

  protected askDelete(idSectors: readonly TableRowId[], sectorName: string | null = null): void {
    this.idSectorsToDelete.set(idSectors.map(String));
    this.sectorNameToDelete.set(sectorName);
    this.deleting.set(true);
  }

  protected askDuplicate(idSectors: readonly TableRowId[], sectorName: string | null = null): void {
    this.idSectorsToDuplicate.set(idSectors.map(String));
    this.sectorNameToDuplicate.set(sectorName);
    this.duplicating.set(true);
  }

  /** Only after "Duplica selezione" (no name): the chosen rows are done with. */
  protected afterDuplicate(): void {
    if (this.sectorNameToDuplicate() === null) {
      this.idSelectedSectors.set([]);
    }
  }

  protected openEdit(sector: SectorItem): void {
    this.sectorToEdit.set({ ...sector });
    this.editing.set(true);
  }

  protected forgetDeleted(idSectors: readonly string[]): void {
    this.idSelectedSectors.update((idSelected) =>
      idSelected.filter((idSelectedSector) => !idSectors.includes(String(idSelectedSector))),
    );
  }
}

/** The single delete of the backend answers `catalog.sector_in_use` without the name of the sector. */
function withSectorName(error: unknown, sectorName: string): unknown {
  if (!(error instanceof HttpErrorResponse)) {
    return error;
  }
  const body: unknown = error.error;
  if (
    typeof body !== 'object' ||
    body === null ||
    !('code' in body) ||
    body.code !== ApiErrorCode.CatalogSectorInUse
  ) {
    return error;
  }
  const args = 'args' in body && typeof body.args === 'object' ? body.args : null;
  return new HttpErrorResponse({
    error: { ...body, args: { sectorName, ...args } },
    headers: error.headers,
    status: error.status,
    statusText: error.statusText,
    url: error.url ?? undefined,
  });
}
