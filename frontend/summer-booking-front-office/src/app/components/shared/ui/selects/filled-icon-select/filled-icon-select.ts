import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';
import { ErrorStateMatcher } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelect, MatSelectModule } from '@angular/material/select';
import { SelectOption } from '../select/select';

export interface IconSelectOption<K extends string = string> extends SelectOption<K> {
  icon: string;
  /** After the name in the list, and alone with the icon in the closed select (e.g. `+39`). */
  shortLabel: string;
}

@Component({
  selector: 'app-filled-icon-select',
  imports: [MatFormFieldModule, MatSelectModule],
  templateUrl: './filled-icon-select.html',
  styleUrl: './filled-icon-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledIconSelect<K extends string = string> {
  readonly label = input<string>();
  /** Asterisk after the label. */
  readonly isMandatory = input(false);
  readonly placeholder = input<string>();
  readonly options = input<readonly IconSelectOption<K>[]>([]);
  readonly value = model<K | null>(null);
  readonly disabled = input(false);
  /** Shown but not changeable, like `disabled`; set by a Signal Forms `[formField]` too. */
  readonly readonly = input(false);
  readonly error = input<string | null>();
  /** The panel was closed: a Signal Forms `[formField]` marks the select as touched. */
  readonly touch = output<void>();

  protected readonly selected = computed(() =>
    this.options().find((option) => option.value === this.value()),
  );
  /** As in the filled text field: the error look follows our `error`, not a classic form control. */
  protected readonly errorMatcher: ErrorStateMatcher = { isErrorState: () => !!this.error() };
  private readonly matSelect = viewChild.required(MatSelect);

  constructor() {
    effect(() => {
      this.error();
      this.matSelect().updateErrorState();
    });
  }
}
