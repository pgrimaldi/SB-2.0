import { ChangeDetectionStrategy, Component, ViewEncapsulation } from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';

@Component({
  selector: 'app-online-booking-services-options-popup',
  imports: [Button, TranslatePipe],
  templateUrl: './online-booking-services-options-popup.html',
  styleUrl: './online-booking-services-options-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class OnlineBookingServicesOptionsPopup extends BasePopup {
  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(44rem, 90vw)',
      maxWidth: 'none',
      maxHeight: '90vh',
      panelClass: 'online__booking__services__options__popup__panel',
    };
  }
}
