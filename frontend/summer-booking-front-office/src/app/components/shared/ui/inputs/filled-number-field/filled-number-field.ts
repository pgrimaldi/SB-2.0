import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  model,
  output,
} from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { resolveIcons } from '../../icons/icons';

let nextId = 0;

const CURRENCY_DECIMALS = 2;

/**
 * Only digits 0-9 can be typed (no signs, no exponent); with `decimal` also one separator, comma or
 * dot.
 */
@Component({
  selector: 'app-filled-number-field',
  imports: [MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './filled-number-field.html',
  styleUrl: './filled-number-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledNumberField {
  readonly label = input<string>();
  readonly value = model<number | null>(null);
  readonly name = input<string>();
  readonly decimal = input(false);
  readonly currency = input(false);
  /** Icons as Material icon names (Material Symbols font): [currency]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [currency]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly error = input<string | null>();
  /** The field was left: a Signal Forms `[formField]` marks it as touched. */
  readonly touch = output<void>();

  protected readonly id = `filled-number-field-${nextId++}`;
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
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

  /** Drops what is not allowed without moving the cursor. */
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

/** Empty text or a lone separator is `null`. */
function toNumber(text: string): number | null {
  const number = text === '' ? NaN : Number(text.replace(',', '.'));
  return Number.isFinite(number) ? number : null;
}
