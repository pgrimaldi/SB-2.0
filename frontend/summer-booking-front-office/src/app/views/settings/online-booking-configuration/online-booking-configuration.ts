import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../../environments/environment';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { OnlineBookingOptionsPanel } from '../../../components/management/settings/online-booking-configuration/online-booking-options-panel/online-booking-options-panel';
import { OnlineBookingServicesOptionsPopup } from '../../../components/management/settings/online-booking-configuration/online-booking-services-options-popup/online-booking-services-options-popup';
import { Button, ButtonAppearance } from '../../../components/shared/ui/buttons/button/button';
import { SectionTitle } from '../../../components/shared/ui/titles/section-title/section-title';
import { SystemService } from '../../../services/api/system/system.service';

const STRIPE_CONNECT_URL = 'https://connect.stripe.com/oauth/authorize';
/** The random `state` sent to Stripe: the return page checks that Stripe gives back the same one. */
export const STRIPE_CONNECT_STATE_KEY = 'sb.stripe-connect-state';

@Component({
  selector: 'app-online-booking-configuration',
  imports: [
    Button,
    OnlineBookingOptionsPanel,
    OnlineBookingServicesOptionsPopup,
    SectionTitle,
    TranslatePipe,
  ],
  templateUrl: './online-booking-configuration.html',
  styleUrl: './online-booking-configuration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnlineBookingConfiguration {
  private readonly auth = inject(AuthBehaviour);
  private readonly system = inject(SystemService);
  private readonly window = inject(DOCUMENT).defaultView;

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly configureStripeIcon = [
    '/assets/images/OnlineBookingConfiguration_Link.svg',
  ] as const;
  protected readonly stepIcon = '/assets/images/OnlineBookingConfiguration_StepChevron.svg';
  protected readonly iframeIcon = '/assets/images/OnlineBookingConfiguration_IframeLink.svg';
  protected readonly copyIcon = ['/assets/images/OnlineBookingConfiguration_Copy.svg'] as const;
  protected readonly closeIcon = ['/assets/images/OnlineBookingConfiguration_Close.svg'] as const;
  protected readonly iframeCode =
    '<iframe src="https://summerbooking.it/property-details/demo/iframe" width="300px" height="350px"></iframe>';
  protected readonly stripeAccountId = signal<string | null>(null);
  protected readonly isBookingOptionsOpen = signal(false);
  protected readonly isServicesOptionsOpen = signal(false);

  constructor() {
    effect((onCleanup) => {
      const idProperty = this.auth.user()?.propertyId;
      if (!idProperty) {
        return;
      }
      const subscription = untracked(() =>
        this.system.stripeInformation(idProperty).subscribe({
          next: (stripeAccountId) => this.stripeAccountId.set(stripeAccountId),
          error: () => this.stripeAccountId.set(null),
        }),
      );
      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected openStripePage(): void {
    const window = this.window;
    if (!window) {
      return;
    }
    const state = window.crypto.randomUUID();
    try {
      window.sessionStorage.setItem(STRIPE_CONNECT_STATE_KEY, state);
    } catch {
      return;
    }
    const params = new URLSearchParams({
      response_type: 'code',
      client_id: environment.stripeClientId,
      scope: 'read_write',
      'stripe_user[country]': 'IT',
      redirect_uri: environment.stripeConnectReturnUrl,
      state,
    });
    window.location.assign(`${STRIPE_CONNECT_URL}?${params}`);
  }
}
