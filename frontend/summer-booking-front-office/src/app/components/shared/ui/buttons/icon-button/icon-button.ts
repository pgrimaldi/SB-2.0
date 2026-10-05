import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { resolveIcons } from '../../icons/icons';

/**
 * A short text can take the place of the icon: `<app-icon-button label="…">S</app-icon-button>`.
 * The button draws the grey circle; icons are just the drawing, without a background.
 */
@Component({
  selector: 'app-icon-button',
  imports: [MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './icon-button.html',
  styleUrl: './icon-button.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IconButton {
  readonly label = input<string>();
  /** Icons as Material icon names (Material Symbols font): [icon]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [icon]. */
  readonly pathIcon = input<readonly string[] | null>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
}
