import { ChangeDetectionStrategy, Component, computed, input, model, signal } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { resolveIcons } from '../../icons/icons';

let nextId = 0;

/** Texts of `app-password-field`, already translated; a missing one is left out. */
export interface PasswordFieldTexts {
  /** Accessible name of the button that shows the password. */
  show?: string;
}

/**
 * Password input with its label above and a button that shows or hides the typed text.
 * Icons (see `Icons`): [password shown, password hidden]. Texts: `PasswordFieldTexts`.
 */
@Component({
  selector: 'app-password-field',
  imports: [MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './password-field.html',
  styleUrl: './password-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PasswordField {
  /** Label above the field, already translated. */
  readonly label = input<string>();
  readonly texts = input<PasswordFieldTexts | null>();
  /** Typed password, two-way: `[(value)]="password"`. */
  readonly value = model('');
  readonly name = input<string>();
  readonly autocomplete = input<string>();
  /** Icons as Material icon names (Material Symbols font): [password shown, password hidden]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [password shown, password hidden]. */
  readonly pathIcon = input<readonly string[] | null>();

  protected readonly id = `password-field-${nextId++}`;
  protected readonly visible = signal(false);
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));

  protected toggle(): void {
    this.visible.update((visible) => !visible);
  }
}
