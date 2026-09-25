import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

let nextId = 0;

/** Single-line input with its label above (Angular Material form field, outlined). */
@Component({
  selector: 'app-text-field',
  imports: [MatFormFieldModule, MatInputModule],
  templateUrl: './text-field.html',
  styleUrl: './text-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TextField {
  readonly label = input.required<string>();
  /** Typed text, two-way: `[(value)]="email"`. */
  readonly value = model('');
  readonly type = input<'text' | 'email'>('text');
  readonly name = input<string>();
  readonly autocomplete = input<string>();

  protected readonly id = `text-field-${nextId++}`;
}
