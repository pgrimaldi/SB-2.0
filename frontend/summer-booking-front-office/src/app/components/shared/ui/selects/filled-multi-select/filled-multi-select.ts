import {
  ChangeDetectionStrategy,
  Component,
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

@Component({
  selector: 'app-filled-multi-select',
  imports: [MatFormFieldModule, MatSelectModule],
  templateUrl: './filled-multi-select.html',
  styleUrl: './filled-multi-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledMultiSelect<K extends string = string> {
  readonly label = input<string>();
  readonly placeholder = input<string>();
  readonly options = input<readonly SelectOption<K>[]>([]);
  readonly disabled = input(false);
  readonly value = model<readonly K[]>([]);
  readonly error = input<string | null>();
  /** The panel was closed: a Signal Forms `[formField]` marks the select as touched. */
  readonly touch = output<void>();

  /** As in the filled select: the error look follows our `error`, not a classic form control. */
  protected readonly errorMatcher: ErrorStateMatcher = { isErrorState: () => !!this.error() };
  private readonly matSelect = viewChild.required(MatSelect);

  constructor() {
    effect(() => {
      this.error();
      this.matSelect().updateErrorState();
    });
  }
}
