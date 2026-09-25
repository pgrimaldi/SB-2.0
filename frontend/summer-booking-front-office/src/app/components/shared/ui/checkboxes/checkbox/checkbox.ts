import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatCheckboxModule } from '@angular/material/checkbox';

/** Checkbox with its label projected as content (Angular Material checkbox). */
@Component({
  selector: 'app-checkbox',
  imports: [MatCheckboxModule],
  templateUrl: './checkbox.html',
  styleUrl: './checkbox.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Checkbox {
  readonly name = input<string>();
  /** Two-way: `[(checked)]="remember"`. */
  readonly checked = model(false);
}
