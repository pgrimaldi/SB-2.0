import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { standardRowActions } from '../../../../components/shared/tables/standard-row-actions';
import { TableErrorPopup } from '../../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import { Table, TableColumn } from '../../../../components/shared/ui/tables/table/table';
import { PageTitle } from '../../../../components/shared/ui/titles/page-title/page-title';
import { HourlyServiceItem } from '../../../../entities/settings/services/hourly-service-item';
import { CatalogService } from '../../../../services/api/catalog/catalog.service';

interface ServicesHourlyTexts {
  service: string;
}

@Component({
  selector: 'app-services-hourly',
  imports: [PageTitle, Table, TableErrorPopup, TranslatePipe],
  templateUrl: './services-hourly.html',
  styleUrl: './services-hourly.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesHourly {
  private readonly auth = inject(AuthBehaviour);
  protected readonly catalog = inject(CatalogService);
  private readonly translateService = inject(TranslateService);

  protected readonly loadFailed = signal(false);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
  protected readonly titleIcon = ['/assets/images/Services_HourlyTitle.png'] as const;
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
      'management.settings.services.hourly_services.table',
    ) as Observable<ServicesHourlyTexts>,
    { initialValue: { service: '' } },
  );
  protected readonly columns = computed<readonly TableColumn<HourlyServiceItem>[]>(() => [
    { field: 'serviceName', header: this.headers().service, sortable: true },
  ]);
  protected readonly rowActions = standardRowActions<HourlyServiceItem>({
    delete: () => {},
    duplicate: () => {},
    edit: () => {},
  });
}
