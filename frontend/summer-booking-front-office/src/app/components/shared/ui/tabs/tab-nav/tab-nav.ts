import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

export interface TabNavLink {
  path: string;
  label: string;
  /** Share of the bar, in percent; the tab never gets narrower than its label. */
  widthPercent?: number;
}

@Component({
  selector: 'app-tab-nav',
  imports: [MatTabsModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './tab-nav.html',
  styleUrl: './tab-nav.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TabNav {
  readonly tabs = input<readonly TabNavLink[]>([]);
}
