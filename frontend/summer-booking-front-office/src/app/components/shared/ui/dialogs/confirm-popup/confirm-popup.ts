import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  effect,
  input,
  output,
} from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { Button } from '../../buttons/button/button';
import { BasePopup } from '../base-popup/base-popup';

let nextId = 0;

export interface ConfirmPopupTexts {
  cancel?: string;
  confirm?: string;
}

/**
 * Confirming does not close the popup: the caller runs the action with `isLoading` on and then closes
 * it, or leaves it open with the `error`.
 */
@Component({
  selector: 'app-confirm-popup',
  imports: [Button],
  templateUrl: './confirm-popup.html',
  styleUrl: './confirm-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class ConfirmPopup extends BasePopup {
  readonly title = input<string>();
  readonly text = input<string>();
  readonly texts = input<ConfirmPopupTexts | null>();
  /** For an action that cannot be undone: the confirm button is red. */
  readonly danger = input(false);
  readonly isLoading = input(false);
  readonly error = input<string | null>();
  readonly confirmed = output<void>();

  protected readonly id = `confirm-popup-${nextId++}`;

  constructor() {
    super();
    effect(() => this.setClosable(!this.isLoading()));
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      role: 'alertdialog',
      ariaLabelledBy: `${this.id}-title`,
      ariaDescribedBy: `${this.id}-text`,
      width: 'min(28rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'confirm__popup__panel',
      // The keyboard starts on Annulla: Enter must not delete by mistake.
      autoFocus: 'first-tabbable',
    };
  }
}
