import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { OnlineBookingAudience } from '../../../../../entities/enums/online-booking-audience';
import { OnlineBookingBlock } from '../../../../../entities/enums/online-booking-block';
import { OnlinePaymentMethod } from '../../../../../entities/enums/online-payment-method';
import { SupplementType } from '../../../../../entities/enums/supplement-type';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { OnlineBookingOptions } from '../../../../../entities/settings/online-booking-configuration/online-booking-options';
import { MapService } from '../../../../../services/api/catalog/map.service';
import { OnlineSettingsService } from '../../../../../services/api/catalog/online-settings.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { OnlineBookingOptionsPanel } from './online-booking-options-panel';

const OPTIONS: OnlineBookingOptions = {
  isEnabled: true,
  isMapPick: false,
  isLocationRestricted: false,
  allowedSpotIds: ['spot1', 'spot2'],
  isPeriodRestricted: false,
  allowedPeriods: [{ startsOn: '2027-05-15T00:00:00.000Z', endsOn: '2027-09-20T00:00:00.000Z' }],
  allowsHalfDay: false,
  morningFrom: '08:00',
  morningTo: '13:00',
  afternoonFrom: null,
  afternoonTo: null,
  afternoonConversionTime: null,
  allowsSameDayBooking: false,
  allowsSeasonal: false,
  allowsSharing: false,
  surchargeType: SupplementType.None,
  surchargeValue: 0,
  onlineBookingBlock: OnlineBookingBlock.ThreeHoursBefore,
  isBlocked: true,
  maxSpotsPerBooking: 4,
  bookingHorizonDays: null,
  requiresManualConfirmation: false,
  scope: OnlineBookingAudience.All,
  maxBookingsPerDay: null,
  stayLimits: [],
  paymentMethods: [OnlinePaymentMethod.PayNow],
  legend: [{ id: 'e1', label: 'Libero', iconId: null, iconUrl: 'https://example.com/old.svg' }],
  goodToKnow: null,
  usefulInfo: null,
  isSuspendedByPlatform: true,
  version: 'v1',
};

@Component({
  imports: [OnlineBookingOptionsPanel],
  template: `<app-online-booking-options-panel [(open)]="open" />`,
})
class OnlineBookingOptionsPanelHost {
  readonly open = signal(false);
}

const problem = (status: number, code: ApiErrorCode) =>
  throwError(() => new HttpErrorResponse({ status, error: { code } }));

describe('OnlineBookingOptionsPanel', () => {
  const setup = async (
    onlineBookingOptions = vi.fn((): Observable<OnlineBookingOptions> => of(OPTIONS)),
    updateOnlineBookingOptions = vi.fn((): Observable<string | null> => of('v2')),
  ) => {
    // jsdom has no matchMedia.
    vi.stubGlobal(
      'matchMedia',
      vi.fn(() => ({
        matches: false,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        addListener: () => undefined,
        removeListener: () => undefined,
      })),
    );
    TestBed.configureTestingModule({
      providers: [
        provideNativeDateAdapter(),
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        {
          provide: OnlineSettingsService,
          useValue: { onlineBookingOptions, updateOnlineBookingOptions },
        },
        { provide: SectorService, useValue: { sectorCombobox: () => of([]) } },
        { provide: MapService, useValue: { iconCatalog: () => of([]) } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    const texts = await import('../../../../../../assets/i18n/it.json');
    translate.setTranslation('it', texts.default);
    translate.use('it');
    const fixture = TestBed.createComponent(OnlineBookingOptionsPanelHost);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const panel = () => document.querySelector<HTMLElement>('.side__panel');
    const checkboxes = () => [
      ...panel()!.querySelectorAll<HTMLInputElement>('app-checkbox input[type="checkbox"]'),
    ];
    const save = () =>
      [...panel()!.querySelectorAll<HTMLButtonElement>('.side__panel__buttons button')].find(
        (button) => button.textContent?.includes('Salva'),
      )!;
    return { fixture, panel, checkboxes, save, updateOnlineBookingOptions };
  };

  it('should send back everything it read, also what it does not show', async () => {
    const { fixture, panel, checkboxes, save, updateOnlineBookingOptions } = await setup();

    expect(panel()!.textContent).toContain('sospesa dalla piattaforma');
    checkboxes()[1].click();
    await fixture.whenStable();
    save().click();
    await fixture.whenStable();

    const { version, isSuspendedByPlatform, ...read } = OPTIONS;
    expect(isSuspendedByPlatform).toBe(true);
    expect(updateOnlineBookingOptions).toHaveBeenCalledWith(
      {
        ...read,
        paymentMethods: [OnlinePaymentMethod.PayNow, OnlinePaymentMethod.PayAtProperty],
      },
      version,
    );
    expect(panel()).toBeNull();
  });

  it('should close and tell it when the options cannot be read', async () => {
    const { panel } = await setup(vi.fn(() => problem(403, ApiErrorCode.AuthForbidden)));

    expect(panel()).toBeNull();
    expect(document.body.textContent).toContain('Riprova');
  });

  it('should close and tell it when another user saved the options meanwhile', async () => {
    const { fixture, panel, checkboxes, save } = await setup(
      undefined,
      vi.fn(() => problem(409, ApiErrorCode.ResourceVersionConflict)),
    );

    checkboxes()[1].click();
    await fixture.whenStable();
    save().click();
    await fixture.whenStable();

    expect(panel()).toBeNull();
    expect(document.body.textContent).toContain('Nel frattempo un altro utente');
  });
});
