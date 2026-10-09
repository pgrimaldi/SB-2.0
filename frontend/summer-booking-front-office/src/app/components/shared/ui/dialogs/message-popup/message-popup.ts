import { ChangeDetectionStrategy, Component, ViewEncapsulation, input } from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { Button } from '../../buttons/button/button';
import { BasePopup } from '../base-popup/base-popup';

export interface MessagePopupTexts {
  close?: string;
}

@Component({
  selector: 'app-message-popup',
  imports: [Button],
  templateUrl: './message-popup.html',
  styleUrl: './message-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class MessagePopup extends BasePopup {
  readonly title = input<string>();
  readonly text = input<string>();
  readonly texts = input<MessagePopupTexts | null>();


  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(28rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'message__popup__panel',
    };
  }
}
