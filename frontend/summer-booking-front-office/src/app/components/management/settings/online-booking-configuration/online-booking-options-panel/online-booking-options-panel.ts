import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';

@Component({
  selector: 'app-online-booking-options-panel',
  imports: [SidePanel, TranslatePipe],
  templateUrl: './online-booking-options-panel.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnlineBookingOptionsPanel {
  readonly open = model(false);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();
}
