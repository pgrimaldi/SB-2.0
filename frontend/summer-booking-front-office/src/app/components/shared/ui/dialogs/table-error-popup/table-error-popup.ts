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

export interface TableErrorPopupTexts {
  close?: string;
  retry?: string;
}

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
  readonly title = input<string>();
  readonly text = input<string>();
  readonly texts = input<TableErrorPopupTexts | null>();
  /** Esc and a click outside still close the popup. */
  readonly hideCloseButton = input(false);
  readonly hideRetryButton = input(false);
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
