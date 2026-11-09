import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  input,
  output,
} from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { BasePopup } from '../base-popup/base-popup';

export interface IconPickerPopupIcon<K extends string = string> {
  value: K;
  url: string;
}

/** Choosing an icon closes the popup. */
@Component({
  selector: 'app-icon-picker-popup',
  templateUrl: './icon-picker-popup.html',
  styleUrl: './icon-picker-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class IconPickerPopup<K extends string = string> extends BasePopup {
  readonly title = input<string>();
  readonly text = input<string>();
  readonly choices = input<readonly IconPickerPopupIcon<K>[]>([]);
  readonly chosenIcon = input<K | null>(null);
  readonly iconChosen = output<K>();

  protected choose(icon: K): void {
    this.iconChosen.emit(icon);
    this.close();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(38rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'icon__picker__popup__panel',
    };
  }
}
