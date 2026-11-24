import { Clipboard } from '@angular/cdk/clipboard';
import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { of, switchMap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AuthBehaviour, STRIPE_CONNECT_STATE_KEY } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import { OnlineBookingOptionsPanel } from '../../../components/management/settings/online-booking-configuration/online-booking-options-panel/online-booking-options-panel';
import { OnlineBookingServicesOptionsPopup } from '../../../components/management/settings/online-booking-configuration/online-booking-services-options-popup/online-booking-services-options-popup';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../components/shared/ui/buttons/button/button';
import { I18nText } from '../../../components/i18n/i18n-text/i18n-text';
import { DATA_RELOAD } from '../../../components/shared/ui/data/data-reload';
import { MessagePopup } from '../../../components/shared/ui/dialogs/message-popup/message-popup';
import { SectionTitle } from '../../../components/shared/ui/titles/section-title/section-title';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { CatalogService } from '../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { PaymentService } from '../../../services/api/payment/payment.service';

const STRIPE_CONNECT_URL = 'https://connect.stripe.com/oauth/authorize';
const PUBLIC_PROPERTY_URL = 'https://summerbooking.it/property-details';

@Component({
  selector: 'app-online-booking-configuration',
  imports: [
    Button,
    I18nText,
    MessagePopup,
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
  private readonly catalog = inject(CatalogService);
  private readonly clipboard = inject(Clipboard);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly payment = inject(PaymentService);
  private readonly window = inject(DOCUMENT).defaultView;

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly configureStripeIcon = [
    '/assets/images/OnlineBookingConfiguration_Link.svg',
  ] as const;
  protected readonly stepIcon = '/assets/images/OnlineBookingConfiguration_StepChevron.svg';
  protected readonly iframeIcon = '/assets/images/OnlineBookingConfiguration_IframeLink.svg';
  protected readonly copyIcon = ['/assets/images/OnlineBookingConfiguration_Copy.svg'] as const;
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  private readonly slugReads = signal(0);
  private readonly slug = signal<string | null>(null);
  protected readonly isSlugRead = signal(false);
  private readonly slugError = signal<ApiProblem | null>(null);
  protected readonly slugErrorMessage = toSignal(
    toObservable(this.slugError).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  /** None while the property has no address on the public site. */
  protected readonly iframeCode = computed(() => {
    const slug = this.slug();
    return slug
      ? `<iframe src="${PUBLIC_PROPERTY_URL}/${encodeURIComponent(slug)}/iframe" title="Summer Booking" width="300" height="350"></iframe>`
      : null;
  });
  protected readonly isCopied = signal(false);
  protected readonly isCopyFailed = signal(false);
  /** Without them Stripe would answer an error page. */
  protected readonly canConnectStripe =
    !!environment.stripeClientId && !!environment.stripeConnectReturnUrl;
  protected readonly isStripeBlocked = signal(false);
  protected readonly stripeAccountId = signal<string | null>(null);
  protected readonly isBookingOptionsOpen = signal(false);
  protected readonly isServicesOptionsOpen = signal(false);

  constructor() {
    effect((onCleanup) => {
      this.dataReload?.();
      if (!this.auth.user()?.propertyId) {
        return;
      }
      const subscription = untracked(() =>
        this.payment.stripeInformation().subscribe({
          next: (stripeAccountId) => this.stripeAccountId.set(stripeAccountId),
          error: () => this.stripeAccountId.set(null),
        }),
      );
      onCleanup(() => subscription.unsubscribe());
    });
    effect((onCleanup) => {
      this.slugReads();
      this.dataReload?.();
      if (!this.auth.user()?.propertyId) {
        return;
      }
      const subscription = untracked(() => {
        this.slugError.set(null);
        this.isSlugRead.set(false);
        return this.catalog.propertySlug().subscribe({
          next: (slug) => {
            this.slug.set(slug);
            this.isSlugRead.set(true);
          },
          error: (error: unknown) => {
            this.slug.set(null);
            this.slugError.set(toApiProblem(error));
          },
        });
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected readSlugAgain(): void {
    this.slugReads.update((reads) => reads + 1);
  }

  protected copyIframeCode(): void {
    const code = this.iframeCode();
    if (code) {
      (this.clipboard.copy(code) ? this.isCopied : this.isCopyFailed).set(true);
    }
  }

  protected openStripePage(): void {
    const window = this.window;
    if (!window) {
      return;
    }
    let state: string;
    try {
      state = window.crypto.randomUUID();
      window.sessionStorage.setItem(STRIPE_CONNECT_STATE_KEY, state);
    } catch {
      this.isStripeBlocked.set(true);
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
