import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';

export type ButtonAppearance = 'primary' | 'secondary';

@Component({
  selector: 'sb-button',
  imports: [MatButtonModule],
  templateUrl: './button.component.html',
  styleUrl: './button.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ButtonComponent {
  readonly appearance = input<ButtonAppearance>('primary');
  readonly disabled = input(false);
  readonly accessibleLabel = input<string>();
  readonly buttonType = input<'button' | 'submit'>('button');
  readonly clicked = output<void>();
}
