import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { SeasonDatesBehaviour } from '../../../behaviours/settings/season-dates.behaviour';
import { SeasonDatesPanel } from '../../../components/management/settings/seasons/season-dates-panel/season-dates-panel';
import { TabNav, TabNavLink } from '../../../components/shared/ui/tabs/tab-nav/tab-nav';
import { SEASONS_TABS } from './seasons-tabs';

@Component({
  selector: 'app-seasons',
  imports: [SeasonDatesPanel, TabNav],
  providers: [SeasonDatesBehaviour],
  templateUrl: './seasons.html',
  styleUrl: './seasons.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Seasons {
  private readonly translateService = inject(TranslateService);

  private readonly labels = toSignal(
    this.translateService.stream(SEASONS_TABS.map(({ label }) => label)) as Observable<
      Partial<Record<string, string>>
    >,
    { initialValue: null },
  );
  protected readonly tabs = computed<readonly TabNavLink[]>(() =>
    SEASONS_TABS.map(({ path, label, widthPercent }) => ({
      path,
      label: this.labels()?.[label] ?? '',
      widthPercent,
    })),
  );
}
