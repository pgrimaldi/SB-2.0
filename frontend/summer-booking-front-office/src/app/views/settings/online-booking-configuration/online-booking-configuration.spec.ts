import { Clipboard } from '@angular/cdk/clipboard';
import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { NO_ERRORS_SCHEMA, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { AuthBehaviour, STRIPE_CONNECT_STATE_KEY } from '../../../behaviours/auth/auth.behaviour';
import { OnlineBookingOptionsPanel } from '../../../components/management/settings/online-booking-configuration/online-booking-options-panel/online-booking-options-panel';
import { OnlineBookingServicesOptionsPopup } from '../../../components/management/settings/online-booking-configuration/online-booking-services-options-popup/online-booking-services-options-popup';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { CatalogService } from '../../../services/api/catalog/catalog.service';
import { PaymentService } from '../../../services/api/payment/payment.service';
import { OnlineBookingConfiguration } from './online-booking-configuration';

describe('OnlineBookingConfiguration', () => {
  const setup = async (propertySlug: () => Observable<string | null> = () => of('lido-demo')) => {
    const copy = vi.fn(() => true);
    const assign = vi.fn();
    const setItem = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
        { provide: CatalogService, useValue: { propertySlug } },
        { provide: PaymentService, useValue: { stripeInformation: () => of(null) } },
        { provide: Clipboard, useValue: { copy } },
      ],
    });
    TestBed.overrideComponent(OnlineBookingConfiguration, {
      remove: { imports: [OnlineBookingOptionsPanel, OnlineBookingServicesOptionsPopup] },
      add: { schemas: [NO_ERRORS_SCHEMA] },
    });
    const translate = TestBed.inject(TranslateService);
    const texts = await import('../../../../assets/i18n/it.json');
    translate.setTranslation('it', texts.default);
    translate.use('it');
    const fixture = TestBed.createComponent(OnlineBookingConfiguration);
    await fixture.whenStable();
    const page = fixture.componentInstance;
    Object.defineProperty(page, 'window', {
      value: {
        crypto: TestBed.inject(DOCUMENT).defaultView!.crypto,
        sessionStorage: { setItem },
        location: { assign },
      },
    });
    return { fixture, page, copy, assign, setItem };
  };

  it('should write the code with the address of the property, whatever its characters, and copy it', async () => {
    const { page, copy } = await setup(() => of('lido "demo"'));

    expect(page['iframeCode']()).toContain(
      'src="https://summerbooking.it/property-details/lido%20%22demo%22/iframe"',
    );
    page['copyIframeCode']();
    expect(copy).toHaveBeenCalledWith(page['iframeCode']());
    expect(page['isCopied']()).toBe(true);

    copy.mockReturnValue(false);
    page['copyIframeCode']();
    expect(page['isCopyFailed']()).toBe(true);
  });

  it('should have no code without an address, and read it again after an error', async () => {
    const propertySlug = vi.fn((): Observable<string | null> =>
      throwError(
        () => new HttpErrorResponse({ status: 403, error: { code: ApiErrorCode.AuthForbidden } }),
      ),
    );
    const { fixture, page, copy } = await setup(propertySlug);

    expect(page['slugErrorMessage']()).toBe('Non hai i permessi per questa operazione.');
    page['copyIframeCode']();
    expect(copy).not.toHaveBeenCalled();

    propertySlug.mockReturnValue(of(null));
    page['readSlugAgain']();
    await fixture.whenStable();
    expect(page['slugErrorMessage']()).toBeNull();
    expect([page['isSlugRead'](), page['iframeCode']()]).toEqual([true, null]);
  });

  it('should go to Stripe with a random state kept in the tab, or say that it cannot be kept', async () => {
    const { page, assign, setItem } = await setup();

    page['openStripePage']();
    const [key, state] = setItem.mock.calls[0] as [string, string];
    expect(key).toBe(STRIPE_CONNECT_STATE_KEY);
    const url = new URL((assign.mock.calls[0] as [string])[0]);
    expect(url.origin + url.pathname).toBe('https://connect.stripe.com/oauth/authorize');
    expect(url.searchParams.get('state')).toBe(state);
    expect(url.searchParams.get('response_type')).toBe('code');

    setItem.mockImplementation(() => {
      throw new Error('blocked');
    });
    page['openStripePage']();
    expect(assign).toHaveBeenCalledTimes(1);
    expect(page['isStripeBlocked']()).toBe(true);
  });
});
