import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  computed,
  input,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { MatDialogConfig } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TextBlockKind, textBlocks } from '../../texts/text-blocks';
import { BasePopup } from '../base-popup/base-popup';

/**
 * What a field or an option means, opened from its (i): icon, title and text, closed by the X. In the
 * text a line break starts a new line, a blank line leaves a space and `**…**` is bold; a line
 * starting with `## ` is a heading, `- ` a point of a list and `  - ` a point under the one above.
 */
@Component({
  selector: 'app-info-popup',
  imports: [MatIconModule, NgTemplateOutlet],
  templateUrl: './info-popup.html',
  styleUrl: './info-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class InfoPopup extends BasePopup {
  readonly title = input<string>();
  readonly text = input<string>();

  protected readonly TextBlockKind = TextBlockKind;
  protected readonly blocks = computed(() => textBlocks(this.text()));

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(28rem, 90vw)',
      maxWidth: 'none',
      maxHeight: '90vh',
      panelClass: 'info__popup__panel',
    };
  }
}
