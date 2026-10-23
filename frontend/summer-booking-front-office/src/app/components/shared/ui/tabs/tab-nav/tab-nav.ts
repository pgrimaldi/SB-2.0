import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import {
  ActivatedRoute,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';

export interface TabNavLink {
  path: string;
  label: string;
  /** Share of the bar, in percent; the tab never gets narrower than its label. */
  widthPercent?: number;
}

@Component({
  selector: 'app-tab-nav',
  imports: [MatSelectModule, MatTabsModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './tab-nav.html',
  styleUrl: './tab-nav.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TabNav {
  readonly tabs = input<readonly TabNavLink[]>([]);

  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private readonly activePath = signal<string | null>(null);
  /** The tab chosen in the dropdown: back to the open one when the navigation does not happen. */
  protected readonly chosenPath = signal<string | null>(null);

  protected setActive(path: string, isActive: boolean): void {
    if (isActive) {
      this.activePath.set(path);
      this.chosenPath.set(path);
    }
  }

  protected choose(path: string | null): void {
    this.chosenPath.set(path);
    if (!path || path === this.activePath()) {
      return;
    }
    void this.router.navigate([path], { relativeTo: this.route }).then((isDone) => {
      if (!isDone) {
        this.chosenPath.set(this.activePath());
      }
    });
  }
}
