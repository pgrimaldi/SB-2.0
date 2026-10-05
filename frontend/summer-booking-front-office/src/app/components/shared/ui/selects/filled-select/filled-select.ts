import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { SelectOption } from '../select/select';

let nextId = 0;

/**
 * Grey select with its label above (Angular Material select in a fill form field), as in the form
 * popups, with the same look as `app-filled-text-field`:
 * `<app-filled-select [label]="…" [placeholder]="…" [options]="options" [(value)]="chosen" />`.
 * Nothing chosen (`null`) shows the placeholder.
 */
@Component({
  selector: 'app-filled-select',
  imports: [MatFormFieldModule, MatSelectModule],
  templateUrl: './filled-select.html',
  styleUrl: './filled-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledSelect<K extends string = string> {
  /** Label above the field, already translated. */
  readonly label = input<string>();
  /** Text shown while nothing is chosen, already translated (e.g. "Seleziona"). */
  readonly placeholder = input<string>();
  /** The choices: `value` is what gets chosen, `label` the text shown (translated). */
  readonly options = input<readonly SelectOption<K>[]>([]);
  /** Chosen value, two-way: `[(value)]="chosen"`; `null` while nothing is chosen. */
  readonly value = model<K | null>(null);
  /** True to show the value without letting it change (e.g. the article of a form to edit). */
  readonly disabled = input(false);

  protected readonly id = `filled-select-${nextId++}`;
}
