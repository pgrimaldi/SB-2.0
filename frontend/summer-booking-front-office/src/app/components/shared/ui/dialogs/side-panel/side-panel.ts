import { Directionality } from '@angular/cdk/bidi';
import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Button, ButtonAppearance, ButtonSize } from '../../buttons/button/button';
import { resolveIcons } from '../../icons/icons';
import { BasePopup } from '../base-popup/base-popup';

export interface SidePanelTexts {
  cancel?: string;
  save?: string;
}

/**
 * Panel sliding in from the side of the page, for forms with many fields. It never closes by itself:
 * X, Annulla, Esc and a click outside send `closeRequest`, and the caller closes it (e.g. after asking
 * about unsaved changes). What is put inside scrolls between the title and the buttons.
 */
@Component({
  selector: 'app-side-panel',
  imports: [Button, MatIconModule],
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
  /** Icons as Material icon names (Material Symbols font): [close]. Without it, no X. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [close]. Without it, no X. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly closeRequest = output<void>();
  readonly save = output<void>();

  private readonly document = inject(DOCUMENT);
  private readonly directionality = inject(Directionality);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));

  protected requestClose(): void {
    if (!this.isLoading()) {
      this.closeRequest.emit();
    }
  }

  protected override closeRequested(): void {
    this.requestClose();
  }

  protected dialogConfig(): MatDialogConfig {
    const isPortrait = this.document.defaultView?.matchMedia('(orientation: portrait)').matches;
    return {
      width: isPortrait ? '100vw' : '60vw',
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
