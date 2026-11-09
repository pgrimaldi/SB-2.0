import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
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
  imports: [PageTitle, Table, TableErrorPopup, TranslatePipe],
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
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
  protected readonly titleIcon = ['/assets/images/GeneralIcon_CalendarFilled.svg'] as const;
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
      'management.settings.rows_and_sectors.table',
    ) as Observable<RowsAndSectorsTableTexts>,
    { initialValue: { sector_name: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<SectorItem>[]>(() => [
    { field: 'sectorName', header: this.headers().sector_name, sortable: true },
  ]);
  protected readonly rowActions = standardRowActions<SectorItem>({
    delete: () => undefined,
    duplicate: () => undefined,
    edit: () => undefined,
  });

  protected readonly load: TableLoad<{ idProperty: string }, SectorItem> = (request) =>
    this.sectors.sectorList(request);
}
