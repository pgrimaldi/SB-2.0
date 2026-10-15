import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
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
}
