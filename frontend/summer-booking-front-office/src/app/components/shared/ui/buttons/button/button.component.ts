import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';

export type ButtonAppearance = 'primary' | 'secondary' | 'light';
// The only four sizes: small 1.75rem, medium 2rem, large 2.875rem (forms), extralarge 3.75rem.
export type ButtonSize = 'small' | 'medium' | 'large' | 'extralarge';

@Component({
  selector: 'app-button',
  imports: [MatButtonModule, NgTemplateOutlet],
  templateUrl: './button.component.html',
  styleUrl: './button.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.button__light]': "appearance() === 'light'",
    '[class.button__small]': "size() === 'small'",
    '[class.button__medium]': "size() === 'medium'",
    '[class.button__large]': "size() === 'large'",
    '[class.button__extralarge]': "size() === 'extralarge'",
    '[class.button__full__width]': 'fullWidth()',
  },
})
export class ButtonComponent {
  readonly appearance = input<ButtonAppearance>('primary');
  readonly size = input<ButtonSize>('medium');
  readonly fullWidth = input(false);
  readonly disabled = input(false);
  readonly accessibleLabel = input<string>();
  readonly buttonType = input<'button' | 'submit'>('button');
  readonly clicked = output<void>();
}
