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
import { MatInput, MatInputModule } from '@angular/material/input';

let nextId = 0;

@Component({
  selector: 'app-filled-textarea',
  imports: [MatFormFieldModule, MatInputModule],
  templateUrl: './filled-textarea.html',
  styleUrl: './filled-textarea.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledTextarea {
  readonly label = input<string>();
  /** Asterisk after the label. */
  readonly isMandatory = input(false);
  /** Shown but not editable; a Signal Forms `[formField]` sets it from the `readonly` rule. */
  readonly readonly = input(false);
  readonly disabled = input(false);
  readonly value = model<string | null>(null);
  readonly rows = input(3);
  readonly maxLength = input<number>();
  readonly name = input<string>();
  readonly placeholder = input<string>();
  readonly error = input<string | null>();
  /** The field was left: a Signal Forms `[formField]` marks it as touched. */
  readonly touch = output<void>();

  /**
   * Material colours the field as wrong from its error state, which it updates only with a classic
   * form control: it follows our `error` instead.
   */
  protected readonly errorMatcher: ErrorStateMatcher = { isErrorState: () => !!this.error() };
  protected readonly id = `filled-textarea-${nextId++}`;
  private readonly matInput = viewChild.required(MatInput);

  constructor() {
    effect(() => {
      this.error();
      this.matInput().updateErrorState();
    });
  }
}
