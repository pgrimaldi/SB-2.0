import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { TabNav, TabNavLink } from '../../../components/shared/ui/tabs/tab-nav/tab-nav';
import { CATALOG_TABS } from './catalog-tabs';

@Component({
  selector: 'app-catalog',
  imports: [TabNav],
  templateUrl: './catalog.html',
  styleUrl: './catalog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Catalog {
  private readonly translateService = inject(TranslateService);

  private readonly labels = toSignal(
    this.translateService.stream(CATALOG_TABS.map(({ label }) => label)) as Observable<
      Partial<Record<string, string>>
    >,
    { initialValue: null },
  );
  protected readonly tabs = computed<readonly TabNavLink[]>(() =>
    CATALOG_TABS.map(({ path, label, widthPercent }) => ({
      path,
      label: this.labels()?.[label] ?? '',
      widthPercent,
    })),
  );
}
