import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';

export type ButtonAppearance = 'primary' | 'secondary' | 'light';
export type ButtonSize = 'small' | 'medium' | 'large';

@Component({
  selector: 'app-button',
  imports: [MatButtonModule, NgTemplateOutlet],
  templateUrl: './button.component.html',
  styleUrl: './button.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.app__button__light]': "appearance() === 'light'",
    '[class.app__button__small]': "size() === 'small'",
    '[class.app__button__medium]': "size() === 'medium'",
    '[class.app__button__large]': "size() === 'large'",
    '[class.app__button__full__width]': 'fullWidth()',
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
