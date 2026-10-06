import {
  ChangeDetectionStrategy,
  Component,
  effect,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';
import { ErrorStateMatcher } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelect, MatSelectModule } from '@angular/material/select';
import { SelectOption } from '../select/select';

let nextId = 0;

@Component({
  selector: 'app-filled-select',
  imports: [MatFormFieldModule, MatSelectModule],
  templateUrl: './filled-select.html',
  styleUrl: './filled-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledSelect<K extends string = string> {
  readonly label = input<string>();
  /** Asterisk after the label; screen readers announce the field as required. */
  readonly isMandatory = input(false);
  readonly placeholder = input<string>();
  readonly options = input<readonly SelectOption<K>[]>([]);
  readonly value = model<K | null>(null);
  readonly disabled = input(false);
  /** Shown but not changeable, like `disabled`; set by a Signal Forms `[formField]` too. */
  readonly readonly = input(false);
  /** Ids of the page elements that name the select, when its label is not above it. */
  readonly labelledBy = input<string>();
  readonly error = input<string | null>();
  /** The panel was closed: a Signal Forms `[formField]` marks the select as touched. */
  readonly touch = output<void>();

  /** As in the filled text field: `aria-invalid` follows our `error`, not a classic form control. */
  protected readonly errorMatcher: ErrorStateMatcher = { isErrorState: () => !!this.error() };
  protected readonly id = `filled-select-${nextId++}`;
  private readonly matSelect = viewChild.required(MatSelect);

  constructor() {
    effect(() => {
      this.error();
      this.matSelect().updateErrorState();
    });
  }
}
