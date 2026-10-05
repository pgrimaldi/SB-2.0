import { ChangeDetectionStrategy, Component, computed, input, model, signal } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { resolveIcons } from '../../icons/icons';

let nextId = 0;

export interface PasswordFieldTexts {
  show?: string;
}

@Component({
  selector: 'app-password-field',
  imports: [MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './password-field.html',
  styleUrl: './password-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PasswordField {
  readonly label = input<string>();
  /** Asterisk after the label; screen readers announce the field as required. */
  readonly isMandatory = input(false);
  readonly texts = input<PasswordFieldTexts | null>();
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
