import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Button, ButtonAppearance } from '../../../components/shared/ui/buttons/button/button';
import { SectionTitle } from '../../../components/shared/ui/titles/section-title/section-title';

@Component({
  selector: 'app-online-booking-configuration',
  imports: [Button, SectionTitle, TranslatePipe],
  templateUrl: './online-booking-configuration.html',
  styleUrl: './online-booking-configuration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnlineBookingConfiguration {
  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly configureStripeIcon = [
    '/assets/images/OnlineBookingConfiguration_Link.svg',
  ] as const;
  protected readonly stepIcon = '/assets/images/OnlineBookingConfiguration_StepChevron.svg';
  protected readonly iframeIcon = '/assets/images/OnlineBookingConfiguration_IframeLink.svg';
  protected readonly copyIcon = ['/assets/images/OnlineBookingConfiguration_Copy.svg'] as const;
}
