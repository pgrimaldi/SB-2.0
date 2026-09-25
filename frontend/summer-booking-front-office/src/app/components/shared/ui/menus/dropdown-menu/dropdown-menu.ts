import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  computed,
  input,
  output,
} from '@angular/core';
import { MatMenuModule } from '@angular/material/menu';

export interface DropdownMenuItem {
  value: string;
  label: string;
  iconSrc?: string;
}

@Component({
  selector: 'app-dropdown-menu',
  imports: [MatMenuModule],
  templateUrl: './dropdown-menu.html',
  styleUrl: './dropdown-menu.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The menu panel is rendered in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class DropdownMenu {
  readonly items = input.required<readonly DropdownMenuItem[]>();
  readonly selectedValue = input<string>();
  readonly accessibleLabel = input.required<string>();
  readonly iconOnly = input(false);
  readonly selected = output<string>();

  protected readonly panelClass = computed(() =>
    this.iconOnly()
      ? 'dropdown__menu__panel dropdown__menu__panel__icon__only'
      : 'dropdown__menu__panel',
  );
}
