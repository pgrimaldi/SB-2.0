import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { DeleteItemsPopup } from '../../../components/management/delete-items-popup/delete-items-popup';
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
  protected readonly idSelectedSectors = signal<readonly TableRowId[]>([]);
  protected readonly deleting = signal(false);
  protected readonly idSectorsToDelete = signal<readonly string[]>([]);
  protected readonly sectorNameToDelete = signal<string | null>(null);
  protected readonly duplicating = signal(false);
  protected readonly idSectorsToDuplicate = signal<readonly string[]>([]);
  protected readonly sectorNameToDuplicate = signal<string | null>(null);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
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
    edit: () => undefined,
  });

  protected readonly load: TableLoad<{ idProperty: string }, SectorItem> = (request) =>
    this.sectors.sectorList(request);

  protected readonly deleteSectors = (idProperty: string, idSectors: string[]) =>
    this.sectors.deleteSectorItems({ idProperty, idSectors });
  protected readonly duplicateSectors = (idProperty: string, idSectors: string[]) =>
    this.sectors.duplicateSectorItems({ idProperty, idSectors });

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

  protected forgetDeleted(idSectors: readonly string[]): void {
    this.idSelectedSectors.update((idSelected) =>
      idSelected.filter((idSelectedSector) => !idSectors.includes(String(idSelectedSector))),
    );
  }
}
