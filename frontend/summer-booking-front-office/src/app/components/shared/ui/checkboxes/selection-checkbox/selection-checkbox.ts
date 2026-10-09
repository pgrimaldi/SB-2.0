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
  readonly isIndeterminate = input(false);
  readonly checked = model(false);
}
