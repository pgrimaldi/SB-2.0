import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { resolveIcons } from '../../icons/icons';

@Component({
  selector: 'app-expansion-panel',
  imports: [MatExpansionModule, MatIconModule],
  templateUrl: './expansion-panel.html',
  styleUrl: './expansion-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpansionPanel {
  readonly title = input<string>();
  readonly expanded = model(false);
  /** Icons as Material icon names (Material Symbols font): [before the title, arrow]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Image paths, used when `matIcon` is not given: [before the title, arrow pointing right]. */
  readonly pathIcon = input<readonly string[] | null>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
}
