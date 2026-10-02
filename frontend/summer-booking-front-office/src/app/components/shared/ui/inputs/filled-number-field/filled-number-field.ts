import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

let nextId = 0;

/**
 * Grey number field with its label above (Angular Material form field, fill), as in the form popups:
 * `<app-filled-number-field [label]="…" [(value)]="quantity" />`. An empty field is `null`.
 */
@Component({
  selector: 'app-filled-number-field',
  imports: [MatFormFieldModule, MatInputModule],
  templateUrl: './filled-number-field.html',
  styleUrl: './filled-number-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledNumberField {
  /** Label above the field, already translated. */
  readonly label = input<string>();
  /** Typed number, two-way: `[(value)]="quantity"`; `null` when the field is empty. */
  readonly value = model<number | null>(null);
  readonly name = input<string>();

  protected readonly id = `filled-number-field-${nextId++}`;

  /** The typed text as a number; empty (or not a number) is `null`. */
  protected changeValue(text: string): void {
    const number = text.trim() === '' ? NaN : Number(text);
    this.value.set(Number.isFinite(number) ? number : null);
  }
}
