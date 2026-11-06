import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  effect,
  input,
  output,
} from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Button, ButtonAppearance } from '../../buttons/button/button';
import { BasePopup } from '../base-popup/base-popup';

export interface WarningPopupTexts {
  cancel?: string;
  confirm?: string;
}

/**
 * Confirming does not close the popup: the caller runs the action with `isLoading` on and then closes
 * it, or leaves it open with the `error`.
 */
@Component({
  selector: 'app-warning-popup',
  imports: [Button, MatIconModule],
  templateUrl: './warning-popup.html',
  styleUrl: './warning-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class WarningPopup extends BasePopup {
  readonly title = input<string>();
  readonly text = input<string>();
  readonly texts = input<WarningPopupTexts | null>();
  readonly isLoading = input(false);
  readonly error = input<string | null>();
  readonly confirmed = output<void>();
  protected readonly ButtonAppearance = ButtonAppearance;

  constructor() {
    super();
    effect(() => this.setClosable(!this.isLoading()));
  }

  protected override isCloseDisabled(): boolean {
    return this.isLoading();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(42rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'warning__popup__panel',
      // The keyboard starts on the cancel button: Enter must not delete by mistake.
      autoFocus: 'first-tabbable',
    };
  }
}
