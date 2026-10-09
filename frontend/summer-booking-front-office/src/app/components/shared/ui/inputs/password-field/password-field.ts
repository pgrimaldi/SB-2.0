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

@Component({
  selector: 'app-password-field',
  imports: [MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './password-field.html',
  styleUrl: './password-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PasswordField {
  readonly label = input<string>();
  /** Asterisk after the label. */
  readonly isMandatory = input(false);
  readonly value = model<string | null>(null);
  readonly name = input<string>();
  readonly autocomplete = input<string>();
  /** Icons as Material icon names (Material Symbols font): [password shown, password hidden]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [password shown, password hidden]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly error = input<string | null>();
  /** The field was left: a Signal Forms `[formField]` marks it as touched. */
  readonly touch = output<void>();

  /**
   * Material colours the field as wrong from its error state, which it updates only with a classic
   * form control: it follows our `error` instead.
   */
  protected readonly errorMatcher: ErrorStateMatcher = { isErrorState: () => !!this.error() };
  protected readonly id = `password-field-${nextId++}`;
  protected readonly visible = signal(false);
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  private readonly matInput = viewChild.required(MatInput);

  constructor() {
    effect(() => {
      this.error();
      this.matInput().updateErrorState();
    });
  }

  protected toggle(): void {
    this.visible.update((visible) => !visible);
  }
}
