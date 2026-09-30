import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { resolveIcons } from '../../icons/icons';

/** Card with an icon on top, a heading (`cardHeading`) and a text. Icons (see `Icons`): [icon]. */
@Component({
  selector: 'app-card',
  imports: [MatCardModule, MatIconModule],
  templateUrl: './card.html',
  styleUrl: './card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Card {
  /** Icons as Material icon names (Material Symbols font): [icon]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [icon]. */
  readonly pathIcon = input<readonly string[] | null>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
}
