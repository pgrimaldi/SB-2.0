import { ChangeDetectionStrategy, Component, input, model, output } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

let nextId = 0;

@Component({
  selector: 'app-filled-textarea',
  imports: [MatFormFieldModule, MatInputModule],
  templateUrl: './filled-textarea.html',
  styleUrl: './filled-textarea.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledTextarea {
  readonly label = input<string>();
  readonly value = model('');
  readonly rows = input(3);
  readonly maxLength = input<number>();
  readonly name = input<string>();
  readonly placeholder = input<string>();
  readonly error = input<string | null>();
  /** The field was left: a Signal Forms `[formField]` marks it as touched. */
  readonly touch = output<void>();

  protected readonly id = `filled-textarea-${nextId++}`;
}
