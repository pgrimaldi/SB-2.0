import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  computed,
  input,
} from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Button } from '../../buttons/button/button';
import { resolveIcons } from '../../icons/icons';
import { BasePopup } from '../base-popup/base-popup';

let nextId = 0;

/** Texts of `app-alert-popup`, already translated; a missing one is left out. */
export interface AlertPopupTexts {
  /** Text of the button that closes the popup (e.g. "Ho capito"). */
  close?: string;
}

/**
 * Generic alert popup (Angular Material dialog): warning icon, title, text and a button that closes it.
 * `<app-alert-popup [title]="…" [text]="…" [texts]="…" [(open)]="failed" />`, all already translated.
 * Screen readers announce it as an alert; Esc, a click outside or the button close it.
 * Icons (see `Icons`): [icon above the title]; without them, no icon. Texts: `AlertPopupTexts`.
 */
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
  /** Title, already translated. */
  readonly title = input<string>();
  /** Text, already translated. */
  readonly text = input<string>();
  readonly texts = input<AlertPopupTexts | null>();
  /** Icons as Material icon names (Material Symbols font): [icon above the title]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [icon above the title]. */
  readonly pathIcon = input<readonly string[] | null>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  protected readonly id = `alert-popup-${nextId++}`;

  protected dialogConfig(): MatDialogConfig {
    return {
      role: 'alertdialog',
      ariaLabelledBy: `${this.id}-title`,
      ariaDescribedBy: `${this.id}-text`,
      width: 'min(28rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'alert__popup__panel',
    };
  }
}
