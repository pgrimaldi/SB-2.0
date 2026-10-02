import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatCheckboxModule } from '@angular/material/checkbox';

/**
 * Checkbox without a visible label that chooses an item (Angular Material checkbox), e.g. the rows
 * of a table: `<app-selection-checkbox [label]="…" [(checked)]="chosen" />`. With `indeterminate`
 * it shows a dash: some of the items it stands for are chosen (e.g. the box of a whole page).
 */
@Component({
  selector: 'app-selection-checkbox',
  imports: [MatCheckboxModule],
  templateUrl: './selection-checkbox.html',
  styleUrl: './selection-checkbox.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SelectionCheckbox {
  /** Accessible name, already translated (there is no visible label). */
  readonly label = input<string>();
  /** Shows a dash instead of the tick: only part of what the box stands for is chosen. */
  readonly indeterminate = input(false);
  /** Two-way: `[(checked)]="chosen"`. */
  readonly checked = model(false);
}
