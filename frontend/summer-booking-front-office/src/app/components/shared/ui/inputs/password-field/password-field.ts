import { ChangeDetectionStrategy, Component, input, model, signal } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslatePipe } from '@ngx-translate/core';

let nextId = 0;

/** Password input with its label above and a button that shows or hides the typed text. */
@Component({
  selector: 'app-password-field',
  imports: [MatFormFieldModule, MatInputModule, TranslatePipe],
  templateUrl: './password-field.html',
  styleUrl: './password-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PasswordField {
  readonly label = input.required<string>();
  /** Typed password, two-way: `[(value)]="password"`. */
  readonly value = model('');
  readonly name = input<string>();
  readonly autocomplete = input<string>();

  protected readonly id = `password-field-${nextId++}`;
  protected readonly visible = signal(false);

  protected toggle(): void {
    this.visible.update((visible) => !visible);
  }
}
