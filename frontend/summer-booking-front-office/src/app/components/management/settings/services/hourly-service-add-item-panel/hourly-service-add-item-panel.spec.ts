import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { HourlyServiceAddItemRequest } from '../../../../../entities/settings/services/hourly-service-add-item-request';
import { HourlyServiceBookingOption } from '../../../../../entities/settings/services/hourly-service-booking-option';
import { Language } from '../../../../../entities/shared/language';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { HourlyServiceAddItemPanel } from './hourly-service-add-item-panel';

const IMAGE = new File(['image'], 'doccia.png', { type: 'image/png' });

describe('HourlyServiceAddItemPanel', () => {
  const setup = async ({
    createHourlyService = vi.fn((request: HourlyServiceAddItemRequest): Observable<string> =>
      of(request ? 's9' : ''),
    ),
    uploadServiceImage = vi.fn((): Observable<void> => of(undefined)),
  } = {}) => {
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:image');
    vi.spyOn(URL, 'revokeObjectURL').mockReturnValue();
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
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
        { provide: CatalogService, useValue: { createHourlyService, uploadServiceImage } },
        { provide: LanguageBehaviour, useValue: { current: signal(Language.It) } },
        {
          provide: SeasonService,
          useValue: {
            periodList: () =>
              of([
                Object.assign(new SeasonPeriodItem(), {
                  key: 3,
                  name: 'Alta',
                  ranges: [
                    { startsOn: '2026-07-01T00:00:00.000Z', endsOn: '2026-08-31T00:00:00.000Z' },
                  ],
                }),
              ]),
          },
        },
        { provide: SectorService, useValue: { sectorCombobox: () => of([]) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      management: {
        settings: {
          services: {
            form: {
              tabs: { general_info: 'Info Generali', prices: 'Prezzi', bookings: 'Prenotazioni' },
              bookings: { time_intervals: {} },
              availability: { days: {}, time_slot_names: {} },
            },
            hourly_services: { categories: {} },
          },
        },
      },
    });
    translate.use('it');
    const fixture = TestBed.createComponent(HourlyServiceAddItemPanel);
    const panel = fixture.componentInstance;
    let created = 0;
    panel.created.subscribe(() => created++);
    fixture.componentRef.setInput('open', true);
    await fixture.whenStable();
    const save = async () => {
      panel['save']();
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };
    const form = panel['draftForm'];
    const fill = async () => {
      form.translations[0].serviceName().value.set('Doccia');
      form.translations[0].description().value.set('Calda');
      form.price().value.set(10);
      form.vatRate().value.set(22);
      await fixture.whenStable();
    };
    return { fixture, panel, form, fill, save, createHourlyService, created: () => created };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should want the name in every language that has a description', async () => {
    const { panel, form, fill } = await setup();

    await fill();
    form.translations[1].description().value.set('Hot shower');
    expect(panel['isSaveDisabled']()).toBe(true);
    expect(form.translations[1].description().errors()).toHaveLength(1);
    form.translations[1].serviceName().value.set('Shower');
    expect(panel['isSaveDisabled']()).toBe(false);
    form.translations[1].description().value.set('');
    form.translations[1].serviceName().value.set('');
    expect(panel['isSaveDisabled']()).toBe(false);
  });

  it('should want the price, and the VAT on the service or on every availability', async () => {
    const { panel, form, fill } = await setup();

    await fill();
    expect(panel['isSaveDisabled']()).toBe(false);
    form.price().value.set(null);
    expect(panel['isSaveDisabled']()).toBe(true);
    form.price().value.set(10);

    form.vatRate().value.set(null);
    expect(form.vatRate().required()).toBe(true);
    expect(panel['isSaveDisabled']()).toBe(true);
    form.availabilityOptions[0].vatRate().value.set(22);
    expect(panel['isSaveDisabled']()).toBe(false);
    form.bookingOptions().value.set([new HourlyServiceBookingOption()]);
    expect(panel['isSaveDisabled']()).toBe(true);
    form.bookingOptions[0].availabilityOption.vatRate().value.set(10);
    expect(panel['isSaveDisabled']()).toBe(false);

    form.isPeriodPricingEnabled().value.set(true);
    expect(panel['isSaveDisabled']()).toBe(true);
    form.periodAvailabilities[0].availabilityOptions[0].vatRate().value.set(22);
    expect(panel['isSaveDisabled']()).toBe(false);
  });

  it('should want the availabilities as the backend, only those in force', async () => {
    const { panel, form, fill } = await setup();

    await fill();
    const option = form.availabilityOptions[0];
    expect(option.daysOfWeek().value()).toEqual([1, 2, 3, 4, 5, 6, 7]);
    const accepts = (change: () => void, undo: () => void) => {
      change();
      const isAccepted = !panel['isSaveDisabled']();
      undo();
      expect(panel['isSaveDisabled']()).toBe(false);
      return isAccepted;
    };
    const days = option.daysOfWeek().value;
    expect(
      accepts(
        () => days.set([]),
        () => days.set([6]),
      ),
    ).toBe(false);
    const isEnabled = option.timeSlotPrices[0].isEnabled().value;
    expect(
      accepts(
        () => isEnabled.set(true),
        () => isEnabled.set(false),
      ),
    ).toBe(false);
    const price = option.timeSlotPrices[0].price().value;
    expect(
      accepts(
        () => price.set(100_001),
        () => price.set(100_000),
      ),
    ).toBe(false);
    const minutes = option.customTimeSlotMinuteQuantity().value;
    const customPrice = option.customTimeSlotPrice().value;
    expect(
      accepts(
        () => customPrice.set(5),
        () => customPrice.set(null),
      ),
    ).toBe(false);
    for (const wrong of [0, 1_441]) {
      expect(
        accepts(
          () => minutes.set(wrong),
          () => minutes.set(1_440),
        ),
      ).toBe(false);
    }
    const isCustomEnabled = option.isCustomTimeSlotEnabled().value;
    expect(
      accepts(
        () => isCustomEnabled.set(true),
        () => customPrice.set(5),
      ),
    ).toBe(false);
    const maximum = option.maxBookingPerTimeSlotQuantity().value;
    expect(
      accepts(
        () => maximum.set(0),
        () => maximum.set(1),
      ),
    ).toBe(false);

    days.set([]);
    form.isPeriodPricingEnabled().value.set(true);
    expect(panel['isSaveDisabled']()).toBe(false);
  });

  it('should send only the availabilities in force', async () => {
    const { panel, form, fill, save, createHourlyService } = await setup({
      createHourlyService: vi.fn(() =>
        throwError(() => new HttpErrorResponse({ status: 500, error: null })),
      ),
    });

    await fill();
    await save();
    expect(createHourlyService.mock.lastCall?.[0]).toMatchObject({
      availabilityOptions: [{ daysOfWeek: [1, 2, 3, 4, 5, 6, 7] }],
      periodAvailabilities: [],
    });

    form.isPeriodPricingEnabled().value.set(true);
    await save();
    expect(createHourlyService.mock.lastCall?.[0]).toMatchObject({
      availabilityOptions: [],
      periodAvailabilities: [{ idPeriod: 3 }],
    });
    expect(panel['draft']().availabilityOptions).toHaveLength(1);
  });

  it('should upload the image chosen to the service just created', async () => {
    const uploadServiceImage = vi.fn((): Observable<void> => of(undefined));
    const { panel, fill, save, created } = await setup({ uploadServiceImage });

    await fill();
    panel['chooseImage'](IMAGE);
    await save();

    expect(uploadServiceImage).toHaveBeenCalledExactlyOnceWith('s9', IMAGE);
    expect(panel.open()).toBe(false);
    expect(created()).toBe(1);
    expect(panel['imageError']()).toBeNull();
  });

  it('should close and say it when the service is created but its image is refused', async () => {
    const { panel, fill, save, created } = await setup({
      uploadServiceImage: vi.fn(() =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 422,
              error: { status: 422, code: ApiErrorCode.CatalogInvalidImage },
            }),
        ),
      ),
    });

    await fill();
    panel['chooseImage'](IMAGE);
    await save();

    expect(panel.open()).toBe(false);
    expect(created()).toBe(1);
    expect(panel['imageError']()?.code).toBe(ApiErrorCode.CatalogInvalidImage);
    expect(document.querySelector('.table__error__popup')).not.toBeNull();
  });
});
