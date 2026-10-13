import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  computed,
  effect,
  input,
  output,
} from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Button, ButtonAppearance } from '../../buttons/button/button';
import { resolveIcons } from '../../icons/icons';
import { BasePopup } from '../base-popup/base-popup';

export interface ConfirmPopupTexts {
  cancel?: string;
  confirm?: string;
}

/**
 * Confirming does not close the popup: the caller runs the action with `isLoading` on and then closes
 * it, or leaves it open with the `error`. What is put inside (e.g. a password field) goes under the
 * text.
 */
@Component({
  selector: 'app-confirm-popup',
  imports: [Button, MatIconModule],
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
  /** Icons as Material icon names (Material Symbols font): [close]. Without it, no X. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [close]. Without it, no X. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly confirmed = output<void>();
  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));

  constructor() {
    super();
    effect(() => this.setClosable(!this.isLoading()));
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(28rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'confirm__popup__panel',
      // The keyboard starts on Annulla: Enter must not delete by mistake.
      autoFocus: 'first-tabbable',
    };
  }
}
