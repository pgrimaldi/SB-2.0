import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  linkedSignal,
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

const CURRENCY_DECIMALS = 2;

export interface FilledNumberFieldTexts {
  /** Name of the eye of a masked field, e.g. "Mostra il contenuto". */
  show?: string;
}

/**
 * Only digits 0-9 can be typed (no signs, no exponent); with `isDecimal` also one separator, comma or
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
  /** Asterisk after the label; screen readers announce the field as required. */
  readonly isMandatory = input(false);
  /** Shown but not editable; a Signal Forms `[formField]` sets it from the `readonly` rule. */
  readonly readonly = input(false);
  readonly disabled = input(false);
  readonly value = model<number | null>(null);
  /** Ids of the page elements that name the field, when its label is not above it. */
  readonly labelledBy = input<string>();
  readonly name = input<string>();
  /** Browser autofill hint; `off` when not given. */
  readonly autocomplete = input<string>();
  readonly isDecimal = input(false);
  readonly isCurrency = input(false);
  /** The digits are dots, as in a password field, with the eye after them to show them. */
  readonly isMasked = input(false);
  /** Icons as Material icon names (Material Symbols font): [currency, content shown, content hidden]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [currency, content shown, content hidden]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly error = input<string | null>();
  readonly texts = input<FilledNumberFieldTexts | null>();
  /** The field was left: a Signal Forms `[formField]` marks it as touched. */
  readonly touch = output<void>();

  /**
   * Material sets `aria-invalid` itself from its error state, which it updates only with a classic
   * form control: it follows our `error` instead.
   */
  protected readonly errorMatcher: ErrorStateMatcher = { isErrorState: () => !!this.error() };
  protected readonly id = `filled-number-field-${nextId++}`;
  private readonly matInput = viewChild.required(MatInput);
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  protected readonly visible = signal(false);
  protected readonly inputType = computed(() =>
    this.isMasked() && !this.visible() ? 'password' : 'text',
  );
  protected readonly decimals = computed(() =>
    !this.isDecimal() ? 0 : this.isCurrency() ? CURRENCY_DECIMALS : Infinity,
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
  constructor() {
    effect(() => {
      this.error();
      this.matInput().updateErrorState();
    });
  }

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

  protected toggleVisible(): void {
    this.visible.update((visible) => !visible);
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
