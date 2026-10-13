import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { resolveIcons } from '../../icons/icons';

export interface FilterChip {
  label: string;
}

export interface FilterChipsTexts {
  clear?: string;
}

@Component({
  selector: 'app-filter-chips',
  imports: [MatIconModule],
  templateUrl: './filter-chips.html',
  styleUrl: './filter-chips.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilterChips<T extends FilterChip = FilterChip> {
  readonly chips = input<readonly T[]>([]);
  readonly texts = input<FilterChipsTexts | null>();
  /** Icons as Material icon names (Material Symbols font): [remove]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [remove]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly removed = output<T>();
  readonly cleared = output<void>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
}
