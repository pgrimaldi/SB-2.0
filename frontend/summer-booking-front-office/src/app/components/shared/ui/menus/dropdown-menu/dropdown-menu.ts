import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  computed,
  input,
  model,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { resolveIcons } from '../../icons/icons';

export interface DropdownMenuItem {
  value: string;
  label: string;
}

@Component({
  selector: 'app-dropdown-menu',
  imports: [MatIconModule, MatMenuModule],
  templateUrl: './dropdown-menu.html',
  styleUrl: './dropdown-menu.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The menu panel is rendered in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class DropdownMenu {
  readonly items = input.required<readonly DropdownMenuItem[]>();
  readonly selected = model('');
  readonly accessibleLabel = input<string>();
  readonly iconOnly = input(false);
  /** Icons as Material icon names (Material Symbols font): one per item, in the order of `items`. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: one per item, in the order of `items`. */
  readonly pathIcon = input<readonly string[] | null>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));

  protected readonly panelClass = computed(() =>
    this.iconOnly()
      ? 'dropdown__menu__panel dropdown__menu__panel__icon__only'
      : 'dropdown__menu__panel',
  );
}
