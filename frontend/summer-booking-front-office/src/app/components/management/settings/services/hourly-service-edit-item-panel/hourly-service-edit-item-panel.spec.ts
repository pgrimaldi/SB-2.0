import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { StandardServiceIcon } from '../../../../../entities/enums/standard-service-icon';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { HourlyServiceSelectedGeneralInfo } from '../../../../../entities/settings/services/hourly-service-selected-general-info';
import { HourlyServiceSelectedBookingOption } from '../../../../../entities/settings/services/hourly-service-selected-booking-option';
import { HourlyServiceSelectedPrices } from '../../../../../entities/settings/services/hourly-service-selected-prices';
import { HourlyServiceUpdateBookingRequest } from '../../../../../entities/settings/services/hourly-service-update-booking-request';
import { HourlyServiceUpdatePricesRequest } from '../../../../../entities/settings/services/hourly-service-update-prices-request';
import { ServiceAvailabilityOption } from '../../../../../entities/settings/services/service-availability-option';
import { ServiceAvailabilityOptionDraft } from '../../../../../entities/settings/services/service-availability-option-draft';
import { HourlyServiceUpdateGeneralRequest } from '../../../../../entities/settings/services/hourly-service-update-general-request';
import { Language } from '../../../../../entities/shared/language';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { HourlyServiceEditItemPanel } from './hourly-service-edit-item-panel';

const IMAGE = new File(['image'], 'doccia.png', { type: 'image/png' });
const SAVED_IMAGE_URL = 'https://files.example.test/doccia.webp';
/** The indexes of the tabs. */
const PRICES = 1;
const BOOKINGS = 2;

const savedAvailability = (values: Partial<ServiceAvailabilityOption>): ServiceAvailabilityOption =>
  Object.assign(new ServiceAvailabilityOption(), { daysOfWeek: [], timeSlotPrices: [] }, values);

describe('HourlyServiceEditItemPanel', () => {
  const setup = async ({
    uploadServiceImage = vi.fn((): Observable<void> => of(undefined)),
    deleteServiceImage = vi.fn((): Observable<void> => of(undefined)),
    generalInfo = {} as Partial<HourlyServiceSelectedGeneralInfo>,
    prices = {} as Partial<HourlyServiceSelectedPrices>,
    bookingOptions = [] as HourlyServiceSelectedBookingOption[],
    periods = [] as SeasonPeriodItem[],
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
    const updateHourlyServiceGeneral = vi.fn(
      (request: HourlyServiceUpdateGeneralRequest): Observable<string | null> =>
        of(request ? 'v2' : null),
    );
    const updateHourlyServicePrices = vi.fn(
      (request: HourlyServiceUpdatePricesRequest): Observable<string | null> =>
        of(request ? 'v2' : null),
    );
    const updateHourlyServiceBooking = vi.fn(
      (request: HourlyServiceUpdateBookingRequest): Observable<string | null> =>
        of(request ? 'v2' : null),
    );
    TestBed.configureTestingModule({
      providers: [
        provideNativeDateAdapter(),
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
        {
          provide: CatalogService,
          useValue: {
            uploadServiceImage,
            deleteServiceImage,
            updateHourlyServiceGeneral,
            updateHourlyServicePrices,
            updateHourlyServiceBooking,
            hourlyServicePrices: () =>
              of(
                Object.assign(new HourlyServiceSelectedPrices(), {
                  price: 10,
                  vatRate: 22,
                  ...prices,
                }),
              ),
            hourlyServiceBookingOptions: () => of({ bookingOptions, version: 'v1' }),
            hourlyServiceGeneralInfo: () =>
              of(
                Object.assign(new HourlyServiceSelectedGeneralInfo(), {
                  idHourlyService: 's5',
                  translations: [
                    { language: Language.It, serviceName: 'Doccia', description: 'Calda' },
                  ],
                  imageUrl: SAVED_IMAGE_URL,
                  version: 'v1',
                  ...generalInfo,
                }),
              ),
          },
        },
        { provide: LanguageBehaviour, useValue: { current: signal(Language.It) } },
        { provide: SeasonService, useValue: { periodList: () => of(periods) } },
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
    const fixture = TestBed.createComponent(HourlyServiceEditItemPanel);
    const panel = fixture.componentInstance;
    let updated = 0;
    panel.updated.subscribe(() => updated++);
    fixture.componentRef.setInput('idHourlyService', 's5');
    fixture.componentRef.setInput('open', true);
    await fixture.whenStable();
    const save = async () => {
      panel['save']();
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };
    return {
      fixture,
      panel,
      save,
      updateHourlyServiceGeneral,
      updateHourlyServicePrices,
      updateHourlyServiceBooking,
      updated: () => updated,
    };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should show the image of the service until an icon or another image is chosen', async () => {
    const { panel } = await setup();

    expect(panel['imagePreviewUrl']()).toBe(SAVED_IMAGE_URL);
    panel['startEdit']();
    panel['chooseIcon'](StandardServiceIcon.Shower);
    expect(panel['imagePreviewUrl']()).toBeNull();
    panel['chooseImage'](IMAGE);
    expect(panel['imagePreviewUrl']()).toBe('blob:image');
    panel['cancelEdit']();
    expect(panel['imagePreviewUrl']()).toBe(SAVED_IMAGE_URL);
  });

  it('should keep the icon in the data when an image is chosen: its upload takes it away', async () => {
    const { panel, save, updateHourlyServiceGeneral } = await setup({
      generalInfo: { icon: StandardServiceIcon.Shower, imageUrl: null },
    });

    panel['startEdit']();
    panel['chooseImage'](IMAGE);
    expect(panel['draftForm'].icon().value()).toBeNull();
    await save();
    expect(updateHourlyServiceGeneral.mock.calls[0][0].icon).toBe(StandardServiceIcon.Shower);
  });

  it('should send back the durations not shown, and every day when none is saved', async () => {
    const otherDuration = { timeSlot: 'FourHours', isEnabled: true, price: 40 };
    const { fixture, panel, save, updateHourlyServicePrices } = await setup({
      prices: {
        availabilityOptions: [
          Object.assign(new ServiceAvailabilityOption(), {
            daysOfWeek: [],
            timeSlotPrices: [otherDuration],
          }),
        ],
      },
    });

    await panel['requestTab'](PRICES);
    await fixture.whenStable();
    expect(panel.hasUnsavedChanges()).toBe(false);
    panel['startEdit']();
    panel['draftForm'].price().value.set(12);
    await save();

    const [option] = updateHourlyServicePrices.mock.calls[0][0].availabilityOptions;
    expect(updateHourlyServicePrices.mock.calls[0][0].price).toBe(12);
    expect(option.daysOfWeek).toEqual([1, 2, 3, 4, 5, 6, 7]);
    expect(option.timeSlotPrices).toHaveLength(9);
    expect(option.timeSlotPrices[8]).toMatchObject(otherDuration);
  });

  it('should not send the empty availabilities it shows where none is saved', async () => {
    const { fixture, panel, save, updateHourlyServicePrices } = await setup({
      periods: [1, 2, 3].map((key) => Object.assign(new SeasonPeriodItem(), { key })),
      prices: {
        isPeriodPricingEnabled: true,
        periodAvailabilities: [
          { idPeriod: 1, availabilityOptions: [savedAvailability({ startTime: '09:00' })] },
          { idPeriod: 2, availabilityOptions: [savedAvailability({})] },
          { idPeriod: 9, availabilityOptions: [savedAvailability({})] },
        ],
      },
    });

    await panel['requestTab'](PRICES);
    await fixture.whenStable();
    panel['startEdit']();
    expect(panel['draft']().periodAvailabilities.map((period) => period.idPeriod)).toEqual([
      1, 2, 3,
    ]);
    panel['draftForm'].periodAvailabilities[2]
      .availabilityOptions()
      .value.update((options) => [...options, new ServiceAvailabilityOptionDraft()]);
    expect(panel.hasUnsavedChanges()).toBe(false);
    panel['draftForm'].price().value.set(12);
    await save();

    const [{ periodAvailabilities }] = updateHourlyServicePrices.mock.calls[0];
    expect(
      periodAvailabilities.map((period) => [period.idPeriod, period.availabilityOptions.length]),
    ).toEqual([
      [1, 1],
      [2, 1],
      [9, 1],
    ]);
  });

  it('should let only a service saved without VAT stay without it', async () => {
    for (const { vatRate, isSaveDisabled } of [
      { vatRate: null, isSaveDisabled: false },
      { vatRate: 22, isSaveDisabled: true },
    ]) {
      TestBed.resetTestingModule();
      const { fixture, panel } = await setup({ prices: { vatRate } });

      await panel['requestTab'](PRICES);
      await fixture.whenStable();
      panel['startEdit']();
      panel['draftForm'].vatRate().value.set(null);
      expect(panel['isSaveDisabled']()).toBe(isSaveDisabled);
    }
  });

  it('should send back what it does not show of a booking option, also without availability', async () => {
    const hidden = { managementStartTime: '08:00', maxPerSlot: 3, intervalMinutes: 20 };
    const { fixture, panel, save, updateHourlyServiceBooking } = await setup({
      bookingOptions: [
        Object.assign(new HourlyServiceSelectedBookingOption(), {
          dateFrom: '2026-07-01T00:00:00.000Z',
          ...hidden,
        }),
      ],
    });

    await panel['requestTab'](BOOKINGS);
    await fixture.whenStable();
    panel['startEdit']();
    panel['draftForm'].bookingOptions[0].dateTo().value.set('2026-07-31T00:00:00.000Z');
    await save();

    expect(updateHourlyServiceBooking.mock.calls[0][0].bookingOptions).toMatchObject([
      {
        dateFrom: '2026-07-01T00:00:00.000Z',
        dateTo: '2026-07-31T00:00:00.000Z',
        ...hidden,
        availabilityOption: null,
      },
    ]);
  });

  it('should say above the buttons that a VAT is not allowed: it can be of an availability', async () => {
    const { fixture, panel, save, updateHourlyServiceBooking } = await setup({
      bookingOptions: [new HourlyServiceSelectedBookingOption()],
    });
    updateHourlyServiceBooking.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 422,
            error: {
              status: 422,
              code: ApiErrorCode.CatalogVatRateNotAllowed,
              args: { rates: [10] },
            },
          }),
      ),
    );

    await panel['requestTab'](BOOKINGS);
    await fixture.whenStable();
    panel['startEdit']();
    panel['draftForm'].bookingOptions[0].availabilityOption.vatRate().value.set(7);
    await save();

    expect(panel['error']()?.code).toBe(ApiErrorCode.CatalogVatRateNotAllowed);
    expect(panel['draftForm'].vatRate().errors()).toHaveLength(0);
  });

  it('should take away an image not saved yet without asking, back to the icon read', async () => {
    const deleteServiceImage = vi.fn((): Observable<void> => of(undefined));
    const { panel } = await setup({
      deleteServiceImage,
      generalInfo: { icon: StandardServiceIcon.Shower, imageUrl: null },
    });

    panel['startEdit']();
    panel['chooseImage'](IMAGE);
    panel['removeImage']();
    expect(panel['imagePreviewUrl']()).toBeNull();
    expect(panel['draftForm'].icon().value()).toBe(StandardServiceIcon.Shower);
    expect(panel.hasUnsavedChanges()).toBe(false);
    expect(panel['isImageDeletionAsked']()).toBe(false);
    expect(deleteServiceImage).not.toHaveBeenCalled();
  });

  it('should delete the saved image only once confirmed, only in Modifica', async () => {
    const deleteServiceImage = vi.fn((): Observable<void> => of(undefined));
    const { panel } = await setup({ deleteServiceImage });

    panel['removeImage']();
    expect(panel['isImageDeletionAsked']()).toBe(false);
    panel['startEdit']();
    panel['removeImage']();
    expect(panel['isImageDeletionAsked']()).toBe(true);
    expect(deleteServiceImage).not.toHaveBeenCalled();

    panel['confirmImageDeletion']();
    expect(deleteServiceImage).toHaveBeenCalledExactlyOnceWith('s5');
    expect(panel['imagePreviewUrl']()).toBeNull();
    expect(panel['isEditing']()).toBe(true);
  });

  it('should keep the image and say why when it cannot be deleted', async () => {
    const { fixture, panel } = await setup({
      deleteServiceImage: vi.fn(() =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 404,
              error: { status: 404, code: ApiErrorCode.ServiceNotFound },
            }),
        ),
      ),
    });
    const failures: string[] = [];
    panel.failed.subscribe(({ code }) => failures.push(code));

    panel['startEdit']();
    panel['removeImage']();
    panel['confirmImageDeletion']();
    await fixture.whenStable();
    expect(panel['imagePreviewUrl']()).toBe(SAVED_IMAGE_URL);
    expect(panel['imageRemovalMessage']()).toBe('error.unknown');
    expect(failures).toEqual([ApiErrorCode.ServiceNotFound]);
  });

  it('should upload the image chosen after the data of its tab', async () => {
    const uploadServiceImage = vi.fn((): Observable<void> => of(undefined));
    const { panel, save, updateHourlyServiceGeneral, updated } = await setup({
      uploadServiceImage,
    });

    panel['startEdit']();
    panel['chooseImage'](IMAGE);
    await save();

    expect(updateHourlyServiceGeneral).toHaveBeenCalledTimes(1);
    expect(uploadServiceImage).toHaveBeenCalledExactlyOnceWith('s5', IMAGE);
    expect(updateHourlyServiceGeneral.mock.invocationCallOrder[0]).toBeLessThan(
      uploadServiceImage.mock.invocationCallOrder[0],
    );
    expect(panel.open()).toBe(false);
    expect(updated()).toBe(1);
  });

  it('should close and say it when the data are saved but the image is refused', async () => {
    const { panel, save, updated } = await setup({
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

    panel['startEdit']();
    panel['chooseImage'](IMAGE);
    await save();

    expect(panel.open()).toBe(false);
    expect(updated()).toBe(1);
    expect(panel['imageError']()?.code).toBe(ApiErrorCode.CatalogInvalidImage);
  });

  it('should send back an icon that is not one of the list, until one is chosen', async () => {
    const { panel, save, updateHourlyServiceGeneral } = await setup({
      generalInfo: { icon: 'Wifi', imageUrl: null },
    });

    expect(panel['draftForm'].icon().value()).toBeNull();
    panel['startEdit']();
    await save();
    expect(updateHourlyServiceGeneral.mock.calls[0][0].icon).toBe('Wifi');

    panel.open.set(true);
    await new Promise((resolve) => setTimeout(resolve));
    panel['startEdit']();
    panel['chooseIcon'](StandardServiceIcon.Pool);
    await save();
    expect(updateHourlyServiceGeneral.mock.calls[1][0].icon).toBe(StandardServiceIcon.Pool);
  });
});
