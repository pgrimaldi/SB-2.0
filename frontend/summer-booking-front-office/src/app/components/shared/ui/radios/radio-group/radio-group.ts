import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatRadioModule } from '@angular/material/radio';

export interface RadioOption<K extends string = string> {
  value: K;
  label: string;
}

/** One choice among a few, all in sight, on one line (on more lines when there is no room). */
@Component({
  selector: 'app-radio-group',
  imports: [MatRadioModule],
  templateUrl: './radio-group.html',
  styleUrl: './radio-group.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RadioGroup<K extends string = string> {
  readonly options = input<readonly RadioOption<K>[]>([]);
  readonly value = model<K | null>(null);
  readonly disabled = input(false);
  /** Shown but not changeable, like `disabled`; set by a Signal Forms `[formField]` too. */
  readonly readonly = input(false);
}
