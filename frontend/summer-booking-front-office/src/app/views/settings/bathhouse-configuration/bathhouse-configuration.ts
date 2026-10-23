import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { BathhouseConfigurationCacheBehaviour } from '../../../behaviours/settings/bathhouse-configuration-cache.behaviour';
import { TabNav, TabNavLink } from '../../../components/shared/ui/tabs/tab-nav/tab-nav';
import { BATHHOUSE_CONFIGURATION_TABS } from './bathhouse-configuration-tabs';

@Component({
  selector: 'app-bathhouse-configuration',
  imports: [TabNav],
  providers: [BathhouseConfigurationCacheBehaviour],
  templateUrl: './bathhouse-configuration.html',
  styleUrl: './bathhouse-configuration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BathhouseConfiguration {
  private readonly translateService = inject(TranslateService);

  private readonly labels = toSignal(
    this.translateService.stream(
      BATHHOUSE_CONFIGURATION_TABS.map(({ label }) => label),
    ) as Observable<Partial<Record<string, string>>>,
    { initialValue: null },
  );
  protected readonly tabs = computed<readonly TabNavLink[]>(() =>
    BATHHOUSE_CONFIGURATION_TABS.map(({ path, label, widthPercent }) => ({
      path,
      label: this.labels()?.[label] ?? '',
      widthPercent,
    })),
  );
}
