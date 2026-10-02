import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

/**
 * On/off switch with its label projected as content (Angular Material slide toggle), small as in
 * the form popups: `<app-toggle [(checked)]="alert">{{ '…' | translate }}</app-toggle>`.
 */
@Component({
  selector: 'app-toggle',
  imports: [MatSlideToggleModule],
  templateUrl: './toggle.html',
  styleUrl: './toggle.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Toggle {
  readonly name = input<string>();
  /** On or off, two-way: `[(checked)]="alert"`. */
  readonly checked = model(false);
}
