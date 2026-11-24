import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { OnlinePaymentMethod } from '../../../../../entities/enums/online-payment-method';
import { SupplementType } from '../../../../../entities/enums/supplement-type';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { OnlineBookingServicesOptions } from '../../../../../entities/settings/online-booking-configuration/online-booking-services-options';
import { OnlineSettingsService } from '../../../../../services/api/catalog/online-settings.service';
import { OnlineBookingServicesOptionsPopup } from './online-booking-services-options-popup';

const UNKNOWN_METHOD = 'bank_transfer' as OnlinePaymentMethod;

const OPTIONS: OnlineBookingServicesOptions = {
  isEnabled: true,
  surchargeType: SupplementType.None,
  surchargeValue: 5,
  requiresManualConfirmation: true,
  sectionTitle: '<p>Prenota i tuoi servizi.</p>',
  usefulInfo: null,
  paymentMethods: [UNKNOWN_METHOD, OnlinePaymentMethod.PayNow],
  version: 'v1',
};

@Component({
  imports: [OnlineBookingServicesOptionsPopup],
  template: `<app-online-booking-services-options-popup [(open)]="open" />`,
})
class OnlineBookingServicesOptionsPopupHost {
  readonly open = signal(false);
}

describe('OnlineBookingServicesOptionsPopup', () => {
  const setup = async (
    updateOnlineServicesOptions = vi.fn((): Observable<string | null> => of('v2')),
  ) => {
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        {
          provide: OnlineSettingsService,
          useValue: { onlineServicesOptions: () => of(OPTIONS), updateOnlineServicesOptions },
        },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      alert: { error_title: 'Operazione non riuscita' },
      management: {
        settings: {
          online_booking_configuration: {
            services_options: {
              save: 'save',
              supplement_types: { none: 'Nessuno', percentage: 'Percentuale', fixed: 'Fisso' },
            },
          },
        },
      },
      rich_text: {},
    });
    translate.use('it');
    const fixture = TestBed.createComponent(OnlineBookingServicesOptionsPopupHost);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const popup = () =>
      document.querySelector<HTMLElement>('.online__booking__services__options__popup');
    const checkboxes = () => [
      ...popup()!.querySelectorAll<HTMLInputElement>('app-checkbox input[type="checkbox"]'),
    ];
    const save = () =>
      [...popup()!.querySelectorAll<HTMLButtonElement>('app-button button')].find((button) =>
        button.textContent?.includes('save'),
      )!;
    return { fixture, popup, checkboxes, save, updateOnlineServicesOptions };
  };

  it('should send back everything it read, with the payment methods and the value it does not show', async () => {
    const { fixture, checkboxes, save, updateOnlineServicesOptions } = await setup();

    expect(checkboxes().map(({ checked }) => checked)).toEqual([true, false, false, false, false]);
    checkboxes()[4].click();
    await fixture.whenStable();
    save().click();
    await fixture.whenStable();

    const { version, ...read } = OPTIONS;
    expect(updateOnlineServicesOptions).toHaveBeenCalledWith(
      {
        ...read,
        paymentMethods: [
          UNKNOWN_METHOD,
          OnlinePaymentMethod.PayNow,
          OnlinePaymentMethod.PrepaidCard,
        ],
      },
      version,
    );
  });

  it('should close and tell it when another user saved the options meanwhile', async () => {
    const { fixture, popup, checkboxes, save } = await setup(
      vi.fn(() =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 409,
              error: { code: ApiErrorCode.ResourceVersionConflict },
            }),
        ),
      ),
    );

    checkboxes()[1].click();
    await fixture.whenStable();
    save().click();
    await fixture.whenStable();

    expect(popup()).toBeNull();
    expect(document.body.textContent).toContain('Operazione non riuscita');
  });
});
