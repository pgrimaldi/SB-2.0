import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { By } from '@angular/platform-browser';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, Subject, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { OnlineBookingAudience } from '../../../../../entities/enums/online-booking-audience';
import { OnlineBookingBlock } from '../../../../../entities/enums/online-booking-block';
import { OnlinePaymentMethod } from '../../../../../entities/enums/online-payment-method';
import { SupplementType } from '../../../../../entities/enums/supplement-type';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { MapCatalogIcon } from '../../../../../entities/settings/map-configuration/map-catalog-icon';
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

const problem = (status: number, code: ApiErrorCode, errors?: object[]) =>
  throwError(() => new HttpErrorResponse({ status, error: { code, errors } }));

describe('OnlineBookingOptionsPanel', () => {
  const setup = async (
    onlineBookingOptions = vi.fn((): Observable<OnlineBookingOptions> => of(OPTIONS)),
    updateOnlineBookingOptions = vi.fn((): Observable<string | null> => of('v2')),
    iconCatalog = (): Observable<MapCatalogIcon[]> => of([]),
  ) => {
    const sectorCombobox = vi.fn(() => of([{ id: 's1', value: 'Fila 1' }]));
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
        { provide: SectorService, useValue: { sectorCombobox } },
        { provide: MapService, useValue: { iconCatalog } },
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
    const component = fixture.debugElement.query(By.directive(OnlineBookingOptionsPanel))
      .componentInstance as OnlineBookingOptionsPanel;
    const form = component['draftForm'];
    const reopen = async () => {
      fixture.componentInstance.open.set(false);
      await fixture.whenStable();
      fixture.componentInstance.open.set(true);
      await fixture.whenStable();
    };
    return {
      fixture,
      panel,
      checkboxes,
      save,
      updateOnlineBookingOptions,
      sectorCombobox,
      component,
      form,
      reopen,
    };
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

  it('should not let the bookings be limited to spots nobody has chosen, nor to no period', async () => {
    const { fixture, panel, save, form } = await setup(
      vi.fn(() => of({ ...OPTIONS, allowedSpotIds: [], allowedPeriods: [] })),
    );
    const switches = [...panel()!.querySelectorAll<HTMLButtonElement>('button[role="switch"]')];

    expect(form.isLocationRestricted().disabled()).toBe(true);
    switches[2].click();
    await fixture.whenStable();

    expect(panel()!.textContent).toContain('Scegli almeno un intervallo di date.');
    expect(save().disabled).toBe(true);
  });

  it('should empty what depends on a choice only when the user changes it', async () => {
    const { fixture, panel, form } = await setup(
      vi.fn(() =>
        of({
          ...OPTIONS,
          allowsHalfDay: true,
          surchargeType: SupplementType.Percentage,
          surchargeValue: 10,
        }),
      ),
    );
    const switches = [...panel()!.querySelectorAll<HTMLButtonElement>('button[role="switch"]')];

    expect([form.morningFrom().value(), form.surchargeValue().value()]).toEqual(['08:00', 10]);
    switches[4].click();
    form.surchargeType().value.set(SupplementType.Fixed);
    await fixture.whenStable();

    expect([form.morningFrom().value(), form.surchargeValue().value()]).toEqual([null, null]);
  });

  it('should check the rules of the limits and of the legend', async () => {
    const { fixture, save, form, component } = await setup();
    const rule = (changes: object) => ({
      id: null,
      sectorId: 's1',
      startsOn: '2027-08-01T00:00:00.000Z',
      endsOn: '2027-08-31T00:00:00.000Z',
      minDays: 5,
      maxDays: 3,
      ...changes,
    });
    const kinds = (index: number) =>
      form.stayLimits[index]()
        .errors()
        .map(({ kind }) => kind);

    form.stayLimits().value.set([rule({})]);
    await fixture.whenStable();
    expect(kinds(0)).toEqual(['online_booking_configuration.booking_limits.max_days']);

    form.stayLimits().value.set([rule({ maxDays: null }), rule({ minDays: null })]);
    await fixture.whenStable();
    expect(kinds(1)).toEqual(['online_booking_configuration.booking_limits.overlap']);

    form.stayLimits().value.set([rule({ minDays: null, maxDays: null })]);
    await fixture.whenStable();
    expect(kinds(0)).toEqual(['online_booking_configuration.booking_limits.days_required']);

    form.stayLimits().value.set([]);
    component['addLegendItem']();
    await fixture.whenStable();
    expect(save().disabled).toBe(true);
  });

  it('should drop the errors of the backend at the next opening', async () => {
    const { fixture, panel, save, reopen } = await setup(
      undefined,
      vi.fn(() =>
        problem(400, ApiErrorCode.ValidationFailed, [
          { field: 'maxBookingsPerDay', code: ApiErrorCode.ValidationOutOfRange },
        ]),
      ),
    );

    save().click();
    await fixture.whenStable();
    expect(panel()!.textContent).toContain("fuori dall'intervallo");
    expect(save().disabled).toBe(true);

    await reopen();
    expect(panel()!.textContent).not.toContain("fuori dall'intervallo");
    expect(save().disabled).toBe(false);
  });

  it('should show an error that is about no field and read the sectors again when one is gone', async () => {
    const { fixture, panel, save, sectorCombobox } = await setup(
      undefined,
      vi.fn(() => problem(404, ApiErrorCode.CatalogSectorNotFound)),
    );

    save().click();
    await fixture.whenStable();

    expect(panel()!.querySelector('.online__booking__options__panel__general')).not.toBeNull();
    expect(sectorCombobox).toHaveBeenCalledTimes(2);
  });

  it('should not open the icons once the panel is closed, and give back the element without its icon', async () => {
    const icons = new Subject<MapCatalogIcon[]>();
    const { fixture, component, form } = await setup(undefined, undefined, () => icons);

    component['pickIcon'](0);
    fixture.componentInstance.open.set(false);
    await fixture.whenStable();
    icons.next([Object.assign(new MapCatalogIcon(), { id: 'i1', url: '/icon.svg' })]);
    await fixture.whenStable();
    expect(document.querySelector('.icon__picker__popup')).toBeNull();

    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    component['pickIcon'](0);
    component['chooseIcon']('i1');
    expect(form.legend[0]().value()).toMatchObject({
      id: 'e1',
      label: 'Libero',
      iconId: 'i1',
      iconUrl: '/icon.svg',
    });
    component['emptyIcon'](form.legend[0]);
    expect(form.legend[0]().value()).toMatchObject({
      id: 'e1',
      label: 'Libero',
      iconId: null,
      iconUrl: null,
    });
  });
});
