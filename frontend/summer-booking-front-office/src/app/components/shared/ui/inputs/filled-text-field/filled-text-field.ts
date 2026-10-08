import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  model,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { ErrorStateMatcher } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInput, MatInputModule } from '@angular/material/input';
import { resolveIcons } from '../../icons/icons';

let nextId = 0;

export interface FilledTextFieldTexts {
  /** Name of the eye of a password field, e.g. "Mostra il contenuto". */
  show?: string;
}

@Component({
  selector: 'app-filled-text-field',
  imports: [MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './filled-text-field.html',
  styleUrl: './filled-text-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledTextField {
  readonly label = input<string>();
  /** Asterisk after the label; screen readers announce the field as required. */
  readonly isMandatory = input(false);
  /** Shown but not editable; a Signal Forms `[formField]` sets it from the `readonly` rule. */
  readonly readonly = input(false);
  readonly disabled = input(false);
  readonly value = model<string | null>(null);
  readonly maxLength = input<number>();
  /** Content hidden as a password, with the eye to show it. */
  readonly isPasswordField = input(false);
  /** Ids of the page elements that name the field, when its label is not above it. */
  readonly labelledBy = input<string>();
  readonly name = input<string>();
  readonly placeholder = input<string>();
  /** Browser autofill hint, e.g. `given-name`, `email`, `tel`. */
  readonly autocomplete = input<string>();
  readonly error = input<string | null>();
  readonly texts = input<FilledTextFieldTexts | null>();
  /** With `isPasswordField`, the eye after the text: [content shown, content hidden]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Image paths, used when `matIcon` is not given: [content shown, content hidden]. */
  readonly pathIcon = input<readonly string[] | null>();
  /** The field was left: a Signal Forms `[formField]` marks it as touched. */
  readonly touch = output<void>();

  /**
   * Material sets `aria-invalid` itself from its error state, which it updates only with a classic
   * form control: it follows our `error` instead.
   */
  protected readonly errorMatcher: ErrorStateMatcher = { isErrorState: () => !!this.error() };
  protected readonly id = `filled-text-field-${nextId++}`;
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  protected readonly visible = signal(false);
  protected readonly inputType = computed(() =>
    this.isPasswordField() && !this.visible() ? 'password' : 'text',
  );
  private readonly matInput = viewChild.required(MatInput);

  constructor() {
    effect(() => {
      this.error();
      this.matInput().updateErrorState();
    });
  }

  protected toggleVisible(): void {
    this.visible.update((visible) => !visible);
  }
}
