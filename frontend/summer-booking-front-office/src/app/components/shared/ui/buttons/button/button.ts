import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { resolveIcons } from '../../icons/icons';

export type ButtonAppearance = 'primary' | 'secondary' | 'light';
// The only four sizes: small 1.75rem, medium 2rem, large 2.875rem (forms), extralarge 3.75rem.
export type ButtonSize = 'small' | 'medium' | 'large' | 'extralarge';

/** Spinner diameter in px. */
const SPINNER_SIZES: Record<ButtonSize, number> = {
  small: 14,
  medium: 16,
  large: 20,
  extralarge: 24,
};

@Component({
  selector: 'app-button',
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule, NgTemplateOutlet],
  templateUrl: './button.html',
  styleUrl: './button.scss',
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
export class Button {
  readonly appearance = input<ButtonAppearance>('primary');
  readonly size = input<ButtonSize>('medium');
  readonly fullWidth = input(false);
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly accessibleLabel = input<string>();
  readonly buttonType = input<'button' | 'submit'>('button');
  /** Icons as Material icon names (Material Symbols font): [icon before the text]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [icon before the text]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly clicked = output<void>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  protected readonly spinnerSize = computed(() => SPINNER_SIZES[this.size()]);

  protected press(): void {
    if (!this.loading()) {
      this.clicked.emit();
    }
  }
}
