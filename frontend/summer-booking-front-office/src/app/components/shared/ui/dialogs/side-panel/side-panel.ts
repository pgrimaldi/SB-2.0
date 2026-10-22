import { Directionality } from '@angular/cdk/bidi';
import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  inject,
  input,
  output,
} from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { Button, ButtonAppearance, ButtonSize } from '../../buttons/button/button';
import { BasePopup } from '../base-popup/base-popup';

/** Portrait screens and phones, also held sideways: there the panel is a popup in the middle. */
const SMALL_SCREEN = '(orientation: portrait), (max-width: 47.99rem), (max-height: 29.99rem)';

export interface SidePanelTexts {
  cancel?: string;
  save?: string;
}

/**
 * Panel sliding in from the side of the page, for forms with many fields; on small screens a popup in
 * the middle. It never closes by itself: X, Annulla, Esc and a click outside send `closeRequest`, and
 * the caller closes it (e.g. after asking about unsaved changes). What is put inside scrolls between
 * the title and the buttons.
 */
@Component({
  selector: 'app-side-panel',
  imports: [Button],
  templateUrl: './side-panel.html',
  styleUrl: './side-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The panel is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class SidePanel extends BasePopup {
  readonly title = input<string>();
  readonly texts = input<SidePanelTexts | null>();
  readonly isLoading = input(false);
  readonly isSaveDisabled = input(false);
  /** Width of the panel, as a percentage of the page; on small screens it is a popup instead. */
  readonly widthPercent = input(60);
  readonly closeRequest = output<void>();
  readonly save = output<void>();

  private readonly document = inject(DOCUMENT);
  private readonly directionality = inject(Directionality);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;

  protected requestClose(): void {
    if (!this.isLoading()) {
      this.closeRequest.emit();
    }
  }

  protected override closeRequested(): void {
    this.requestClose();
  }

  protected override isCloseDisabled(): boolean {
    return this.isLoading();
  }

  protected dialogConfig(): MatDialogConfig {
    if (this.document.defaultView?.matchMedia(SMALL_SCREEN).matches) {
      return {
        width: 'min(44rem, 90vw)',
        maxWidth: 'none',
        maxHeight: '90vh',
        panelClass: ['side__panel__panel', 'side__panel__popup'],
        disableClose: true,
        autoFocus: 'dialog',
      };
    }
    return {
      width: `${this.widthPercent()}vw`,
      maxWidth: 'none',
      height: '100%',
      maxHeight: 'none',
      position: this.directionality.value === 'rtl' ? { left: '0' } : { right: '0' },
      panelClass: 'side__panel__panel',
      enterAnimationDuration: 0,
      exitAnimationDuration: 0,
      disableClose: true,
      autoFocus: 'dialog',
    };
  }
}
