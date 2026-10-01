import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  input,
  output,
} from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { Button } from '../../buttons/button/button';
import { BasePopup } from '../base-popup/base-popup';

let nextId = 0;

/** Texts of `app-table-error-popup`, already translated; a missing one is left out. */
export interface TableErrorPopupTexts {
  /** Text of the button that only closes the popup (e.g. "Chiudi"). */
  close?: string;
  /** Text of the button that closes the popup and asks to load the data again (e.g. "Riprova"). */
  retry?: string;
}

/**
 * Popup for data that could not be loaded (Angular Material dialog), for every table: title, text
 * and two buttons, "close" on the left and "retry" on the right. Retry closes it and emits `retry`:
 * the page loads the data again (e.g. `table.reload()`).
 * `<app-table-error-popup [title]="…" [text]="…" [texts]="…" [(open)]="failed" (retry)="…" />`,
 * all already translated. Screen readers announce it as an alert; Esc or a click outside close it.
 */
@Component({
  selector: 'app-table-error-popup',
  imports: [Button],
  templateUrl: './table-error-popup.html',
  styleUrl: './table-error-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class TableErrorPopup extends BasePopup {
  /** Title, already translated. */
  readonly title = input<string>();
  /** Text, already translated. */
  readonly text = input<string>();
  readonly texts = input<TableErrorPopupTexts | null>();
  /** Hides the close button (Esc and a click outside still close the popup). */
  readonly hideCloseButton = input(false);
  /** Hides the retry button. */
  readonly hideRetryButton = input(false);
  /** The user asked to load the data again (the popup is already closing). */
  readonly retry = output<void>();

  protected readonly id = `table-error-popup-${nextId++}`;

  protected retryLoad(): void {
    this.close();
    this.retry.emit();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      role: 'alertdialog',
      ariaLabelledBy: `${this.id}-title`,
      ariaDescribedBy: `${this.id}-text`,
      width: 'min(28rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'table__error__popup__panel',
    };
  }
}
