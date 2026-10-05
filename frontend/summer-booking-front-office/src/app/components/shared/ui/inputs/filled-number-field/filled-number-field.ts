import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  model,
} from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { resolveIcons } from '../../icons/icons';

let nextId = 0;

/** Most digits after the separator of an amount (cents). */
const CURRENCY_DECIMALS = 2;

/**
 * Grey number field with its label above (Angular Material form field, fill), as in the form popups:
 * `<app-filled-number-field [label]="…" [(value)]="quantity" />`. An empty field is `null`.
 * Only digits 0-9 can be typed (no signs, no exponent); with `decimal` also one separator, comma or
 * dot. With `currency` the value is an amount: at most 2 digits after the separator and the currency
 * icon before the number.
 * Icons (see `Icons`): [currency], shown only with `currency`.
 */
@Component({
  selector: 'app-filled-number-field',
  imports: [MatFormFieldModule, MatIconModule, MatInputModule],
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
  /** True to accept a number with a decimal part (comma or dot); otherwise whole numbers only. */
  readonly decimal = input(false);
  /** True when the number is an amount of money: currency icon, at most 2 decimal digits. */
  readonly currency = input(false);
  /** Icons as Material icon names (Material Symbols font): [currency]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [currency]. */
  readonly pathIcon = input<readonly string[] | null>();

  protected readonly id = `filled-number-field-${nextId++}`;
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  /** Digits allowed after the separator: none, 2 for an amount, otherwise any. */
  protected readonly decimals = computed(() =>
    !this.decimal() ? 0 : this.currency() ? CURRENCY_DECIMALS : Infinity,
  );
  /**
   * Text in the field. It stays as typed while it means the same number (e.g. "3," while typing
   * "3,5"); a number given from outside is written again.
   */
  protected readonly text = linkedSignal<number | null, string>({
    source: this.value,
    computation: (value, previous) =>
      previous && toNumber(previous.value) === value ? previous.value : (value?.toString() ?? ''),
  });

  /** Drops what is not allowed (letters, signs, extra separators), keeps the cursor, sets the value. */
  protected changeValue(field: HTMLInputElement): void {
    const decimals = this.decimals();
    const text = keepNumber(field.value, decimals);
    if (text !== field.value) {
      const cursor = keepNumber(field.value.slice(0, field.selectionStart ?? 0), decimals).length;
      field.value = text;
      field.setSelectionRange(cursor, cursor);
    }
    this.text.set(text);
    this.value.set(toNumber(text));
  }
}

/** Only digits, and with decimals one separator (comma or dot) followed by at most `decimals` digits. */
function keepNumber(text: string, decimals: number): string {
  let kept = '';
  let separator = false;
  let fraction = 0;
  for (const char of text) {
    if (char >= '0' && char <= '9') {
      if (separator && fraction++ >= decimals) {
        continue;
      }
      kept += char;
    } else if ((char === ',' || char === '.') && decimals > 0 && !separator) {
      separator = true;
      kept += char;
    }
  }
  return kept;
}

/** The kept text as a number; empty (or a lone separator) is `null`. */
function toNumber(text: string): number | null {
  const number = text === '' ? NaN : Number(text.replace(',', '.'));
  return Number.isFinite(number) ? number : null;
}
