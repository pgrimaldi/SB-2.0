import { ChangeDetectionStrategy, Component, ViewEncapsulation, input } from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Button } from '../../buttons/button/button';
import { BasePopup } from '../base-popup/base-popup';

export interface AlertPopupTexts {
  close?: string;
}

@Component({
  selector: 'app-alert-popup',
  imports: [Button, MatIconModule],
  templateUrl: './alert-popup.html',
  styleUrl: './alert-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class AlertPopup extends BasePopup {
  readonly title = input<string>();
  readonly text = input<string>();
  readonly texts = input<AlertPopupTexts | null>();

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(28rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'alert__popup__panel',
    };
  }
}
