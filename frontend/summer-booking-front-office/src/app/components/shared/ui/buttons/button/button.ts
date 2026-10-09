import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { resolveIcons } from '../../icons/icons';

export enum ButtonAppearance {
  Primary,
  Secondary,
  Light,
}

/** The only four sizes: small 1.75rem, medium 2rem, large 2.875rem (forms), extralarge 3.75rem. */
export enum ButtonSize {
  Small,
  Medium,
  Large,
  ExtraLarge,
}

/** Spinner diameter in px. */
const SPINNER_SIZES: Record<ButtonSize, number> = {
  [ButtonSize.Small]: 14,
  [ButtonSize.Medium]: 16,
  [ButtonSize.Large]: 20,
  [ButtonSize.ExtraLarge]: 24,
};

@Component({
  selector: 'app-button',
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule, NgTemplateOutlet],
  templateUrl: './button.html',
  styleUrl: './button.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.button__light]': 'appearance() === ButtonAppearance.Light',
    '[class.button__small]': 'size() === ButtonSize.Small',
    '[class.button__medium]': 'size() === ButtonSize.Medium',
    '[class.button__large]': 'size() === ButtonSize.Large',
    '[class.button__extralarge]': 'size() === ButtonSize.ExtraLarge',
    '[class.button__full__width]': 'isFullWidth()',
  },
})
export class Button {
  readonly appearance = input(ButtonAppearance.Primary);
  readonly size = input(ButtonSize.Medium);
  readonly isFullWidth = input(false);
  readonly disabled = input(false);
  readonly isLoading = input(false);
  /** A submit button of its form instead of a plain button. */
  readonly isSubmitButton = input(false);
  /** Icons as Material icon names (Material Symbols font): [icon before the text]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [icon before the text]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly clicked = output<void>();

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  protected readonly spinnerSize = computed(() => SPINNER_SIZES[this.size()]);

  protected press(): void {
    if (!this.isLoading()) {
      this.clicked.emit();
    }
  }
}
