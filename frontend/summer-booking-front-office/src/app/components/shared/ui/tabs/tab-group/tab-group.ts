import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';

export interface TabGroupTab {
  label: string;
  /** Share of the bar, in percent; the tab never gets narrower than its label. */
  widthPercent?: number;
}

/**
 * Tabs inside a page or a panel, without routes: the caller shows under them the content of the open
 * tab (`selectedIndex`).
 */
@Component({
  selector: 'app-tab-group',
  imports: [MatSelectModule, MatTabsModule],
  templateUrl: './tab-group.html',
  styleUrl: './tab-group.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TabGroup {
  readonly tabs = input<readonly TabGroupTab[]>([]);
  readonly selectedIndex = model(0);
}
