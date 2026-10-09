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
  selector: 'app-text-field',
  imports: [MatFormFieldModule, MatInputModule],
  templateUrl: './text-field.html',
  styleUrl: './text-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TextField {
  readonly label = input<string>();
  /** Asterisk after the label. */
  readonly isMandatory = input(false);
  readonly value = model<string | null>(null);
  /** An email address: phones show the keyboard for it. */
  readonly isEmailField = input(false);
  readonly name = input<string>();
  readonly autocomplete = input<string>();
  readonly error = input<string | null>();
  /** The field was left: a Signal Forms `[formField]` marks it as touched. */
  readonly touch = output<void>();

  /**
   * Material colours the field as wrong from its error state, which it updates only with a classic
   * form control: it follows our `error` instead.
   */
  protected readonly errorMatcher: ErrorStateMatcher = { isErrorState: () => !!this.error() };
  protected readonly id = `text-field-${nextId++}`;
  private readonly matInput = viewChild.required(MatInput);

  constructor() {
    effect(() => {
      this.error();
      this.matInput().updateErrorState();
    });
  }
}
