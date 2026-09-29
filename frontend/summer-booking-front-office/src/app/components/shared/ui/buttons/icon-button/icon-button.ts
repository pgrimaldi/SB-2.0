import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Round icon button (Angular Material icon button) with a tooltip:
 * `<app-icon-button label="…" icon="/assets/images/settings.svg" />`, or a short text in place of
 * the icon: `<app-icon-button label="…">S</app-icon-button>`.
 * The button draws the grey circle; icons are just the drawing, without a background.
 */
@Component({
  selector: 'app-icon-button',
  imports: [MatButtonModule, MatTooltipModule, TranslatePipe],
  templateUrl: './icon-button.html',
  styleUrl: './icon-button.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IconButton {
  /** Translation key: accessible name and tooltip. */
  readonly label = input.required<string>();
  readonly icon = input<string>();
}
