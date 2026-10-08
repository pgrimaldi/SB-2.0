import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatCheckboxModule } from '@angular/material/checkbox';

@Component({
  selector: 'app-selection-checkbox',
  imports: [MatCheckboxModule],
  templateUrl: './selection-checkbox.html',
  styleUrl: './selection-checkbox.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SelectionCheckbox {
  /** Accessible name only: there is no visible label. */
  readonly label = input<string>();
  readonly isIndeterminate = input(false);
  readonly checked = model(false);
}
