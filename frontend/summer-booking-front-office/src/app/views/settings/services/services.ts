import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { TabNav, TabNavLink } from '../../../components/shared/ui/tabs/tab-nav/tab-nav';
import { SERVICES_TABS } from './services-tabs';

@Component({
  selector: 'app-services',
  imports: [TabNav],
  templateUrl: './services.html',
  styleUrl: './services.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Services {
  private readonly translateService = inject(TranslateService);

  private readonly labels = toSignal(
    this.translateService.stream(SERVICES_TABS.map(({ label }) => label)) as Observable<
      Partial<Record<string, string>>
    >,
    { initialValue: null },
  );
  protected readonly tabs = computed<readonly TabNavLink[]>(() =>
    SERVICES_TABS.map(({ path, label, widthPercent }) => ({
      path,
      label: this.labels()?.[label] ?? '',
      widthPercent,
    })),
  );
}
