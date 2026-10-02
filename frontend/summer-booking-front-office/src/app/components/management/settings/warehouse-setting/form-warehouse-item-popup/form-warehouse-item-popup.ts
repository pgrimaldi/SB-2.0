import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  effect,
  signal,
  untracked,
} from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { Button } from '../../../../shared/ui/buttons/button/button';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';

let nextId = 0;

/**
 * Popup of the warehouse settings to add an article: name, quantity, threshold and the switch of the
 * threshold alert, then Annulla or Aggiungi. `<app-form-warehouse-item-popup [(open)]="adding" />`.
 * For now only the form: no checks on the fields, and Aggiungi does nothing yet (the API comes later).
 */
@Component({
  selector: 'app-form-warehouse-item-popup',
  imports: [Button, FilledNumberField, FilledTextField, Toggle, TranslatePipe],
  templateUrl: './form-warehouse-item-popup.html',
  styleUrl: './form-warehouse-item-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class FormWarehouseItemPopup extends BasePopup {
  /** Name of the article, up to 500 characters. */
  protected readonly name = signal('');
  protected readonly quantity = signal<number | null>(null);
  /** Threshold of the article; empty is `null`. */
  protected readonly threshold = signal<number | null>(null);
  /** The threshold alert, on or off (independent of the threshold). */
  protected readonly thresholdAlert = signal(false);
  protected readonly closeIcon = '/assets/images/close.svg';
  protected readonly id = `form-warehouse-item-popup-${nextId++}`;

  constructor() {
    super();
    // Every opening starts from an empty form.
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      role: 'dialog',
      ariaLabelledBy: `${this.id}-title`,
      width: 'min(25rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'form__warehouse__item__popup__panel',
      // The keyboard starts in the first field, not on the X.
      autoFocus: 'input',
    };
  }

  private reset(): void {
    this.name.set('');
    this.quantity.set(null);
    this.threshold.set(null);
    this.thresholdAlert.set(false);
  }
}
