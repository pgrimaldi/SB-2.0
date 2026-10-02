import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

let nextId = 0;

/**
 * Grey single-line text field with its label above (Angular Material form field, fill), as in the
 * form popups: `<app-filled-text-field [label]="…" [maxLength]="500" [(value)]="name" />`.
 */
@Component({
  selector: 'app-filled-text-field',
  imports: [MatFormFieldModule, MatInputModule],
  templateUrl: './filled-text-field.html',
  styleUrl: './filled-text-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledTextField {
  /** Label above the field, already translated. */
  readonly label = input<string>();
  /** Typed text, two-way: `[(value)]="name"`. */
  readonly value = model('');
  /** Most characters that can be typed; without it, no limit. */
  readonly maxLength = input<number>();
  readonly name = input<string>();

  protected readonly id = `filled-text-field-${nextId++}`;
}
