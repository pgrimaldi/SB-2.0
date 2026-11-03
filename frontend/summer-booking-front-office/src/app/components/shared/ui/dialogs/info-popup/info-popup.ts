import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  computed,
  input,
} from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { boldParts } from '../../texts/bold-text';
import { BasePopup } from '../base-popup/base-popup';

/**
 * What a field or an option means, opened from its (i): icon, title and text, closed by the X. In the
 * text a line break starts a new line and `**…**` is bold.
 */
@Component({
  selector: 'app-info-popup',
  imports: [MatIconModule],
  templateUrl: './info-popup.html',
  styleUrl: './info-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class InfoPopup extends BasePopup {
  readonly title = input<string>();
  readonly text = input<string>();

  protected readonly textParts = computed(() => boldParts(this.text()));

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(28rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'info__popup__panel',
    };
  }
}
