import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { SelectOption } from '../select/select';

let nextId = 0;

@Component({
  selector: 'app-filled-select',
  imports: [MatFormFieldModule, MatSelectModule],
  templateUrl: './filled-select.html',
  styleUrl: './filled-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledSelect<K extends string = string> {
  readonly label = input<string>();
  readonly placeholder = input<string>();
  readonly options = input<readonly SelectOption<K>[]>([]);
  readonly value = model<K | null>(null);
  readonly disabled = input(false);

  protected readonly id = `filled-select-${nextId++}`;
}
