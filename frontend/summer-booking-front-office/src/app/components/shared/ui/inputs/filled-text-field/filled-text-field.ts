import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

let nextId = 0;

@Component({
  selector: 'app-filled-text-field',
  imports: [MatFormFieldModule, MatInputModule],
  templateUrl: './filled-text-field.html',
  styleUrl: './filled-text-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledTextField {
  readonly label = input<string>();
  readonly value = model('');
  readonly maxLength = input<number>();
  readonly name = input<string>();

  protected readonly id = `filled-text-field-${nextId++}`;
}
