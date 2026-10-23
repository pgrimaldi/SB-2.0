import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, TemplateRef, inject } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Icons } from '../../icons/icons';

export interface PopupFrameData {
  readonly content: TemplateRef<unknown>;
  readonly icons: () => Icons;
  readonly isCloseDisabled: () => boolean;
  readonly close: () => void;
}

/**
 * What `BasePopup` opens: the content of the popup with the X in the top corner, the same in every
 * popup. The X comes after the content, so the keyboard starts on the content.
 */
@Component({
  selector: 'app-popup-frame',
  imports: [MatIconModule, NgTemplateOutlet],
  templateUrl: './popup-frame.html',
  styleUrl: './popup-frame.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PopupFrame {
  protected readonly data = inject<PopupFrameData>(MAT_DIALOG_DATA);
}
