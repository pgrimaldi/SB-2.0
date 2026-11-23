import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { StandardServiceIcon } from '../../../../../entities/enums/standard-service-icon';
import { Weekday } from '../../../../../entities/enums/weekday';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { StandardServiceSelectedGeneralInfo } from '../../../../../entities/settings/services/standard-service-selected-general-info';
import { StandardServiceSelectedPrices } from '../../../../../entities/settings/services/standard-service-selected-prices';
import { StandardServiceUpdateBookingRequest } from '../../../../../entities/settings/services/standard-service-update-booking-request';
import { StandardServiceUpdatePricesRequest } from '../../../../../entities/settings/services/standard-service-update-prices-request';
import { StandardServiceUpdateGeneralRequest } from '../../../../../entities/settings/services/standard-service-update-general-request';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { ServiceBookingOption } from '../../../../../entities/settings/services/service-booking-option';
import { Language } from '../../../../../entities/shared/language';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { GeneralServiceEditItemPanel } from './general-service-edit-item-panel';

const IMAGE = new File(['image'], 'doccia.png', { type: 'image/png' });
const SAVED_IMAGE_URL = 'https://files.example.test/doccia.webp';
const BOOKING_OPTION = Object.assign(new ServiceBookingOption(), {
  managementStartTime: '09:00',
  managementEndTime: '12:00',
  weekdays: [Weekday.Saturday, Weekday.Sunday],
  maxPerSlot: 5,
  intervalMinutes: 20,
});

describe('GeneralServiceEditItemPanel', () => {
  const setup = async ({
    uploadServiceImage = vi.fn((): Observable<void> => of(undefined)),
    deleteServiceImage = vi.fn((): Observable<void> => of(undefined)),
    generalInfo = {} as Partial<StandardServiceSelectedGeneralInfo>,
    prices = {} as Partial<StandardServiceSelectedPrices>,
    periods = [] as readonly SeasonPeriodItem[],
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
    const updateStandardServiceGeneral = vi.fn(
      (request: StandardServiceUpdateGeneralRequest): Observable<string | null> =>
        of(request ? 'v2' : null),
    );
    const updateStandardServiceBooking = vi.fn(
      (request: StandardServiceUpdateBookingRequest): Observable<string | null> =>
        of(request ? 'v2' : null),
    );
    const updateStandardServicePrices = vi.fn(
      (request: StandardServiceUpdatePricesRequest): Observable<string | null> =>
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
            updateStandardServiceGeneral,
            updateStandardServiceBooking,
            updateStandardServicePrices,
            standardServiceBookingOptions: () =>
              of({ bookingOptions: [BOOKING_OPTION], version: 'v1' }),
            standardServiceGeneralInfo: () =>
              of(
                Object.assign(new StandardServiceSelectedGeneralInfo(), {
                  idStandardService: 's5',
                  translations: [
                    { language: Language.It, serviceName: 'Doccia', description: 'Calda' },
                  ],
                  imageUrl: SAVED_IMAGE_URL,
                  version: 'v1',
                  ...generalInfo,
                }),
              ),
            standardServicePrices: () =>
              of(
                Object.assign(new StandardServiceSelectedPrices(), {
                  price: 3,
                  version: 'v1',
                  ...prices,
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
            },
            standard_services: { categories: {} },
          },
        },
      },
    });
    translate.use('it');
    const fixture = TestBed.createComponent(GeneralServiceEditItemPanel);
    const panel = fixture.componentInstance;
    let updated = 0;
    panel.updated.subscribe(() => updated++);
    fixture.componentRef.setInput('idStandardService', 's5');
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
      updateStandardServiceGeneral,
      updateStandardServiceBooking,
      updateStandardServicePrices,
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
    const { panel, save, updateStandardServiceGeneral } = await setup({
      generalInfo: { icon: StandardServiceIcon.Shower, imageUrl: null },
    });

    panel['startEdit']();
    panel['chooseImage'](IMAGE);
    expect(panel['draftForm'].icon().value()).toBeNull();
    await save();
    expect(updateStandardServiceGeneral.mock.calls[0][0].icon).toBe(StandardServiceIcon.Shower);
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
    const { panel, save, updateStandardServiceGeneral, updated } = await setup({
      uploadServiceImage,
    });

    panel['startEdit']();
    panel['chooseImage'](IMAGE);
    await save();

    expect(updateStandardServiceGeneral).toHaveBeenCalledTimes(1);
    expect(uploadServiceImage).toHaveBeenCalledExactlyOnceWith('s5', IMAGE);
    expect(updateStandardServiceGeneral.mock.invocationCallOrder[0]).toBeLessThan(
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
    const { panel, save, updateStandardServiceGeneral } = await setup({
      generalInfo: { icon: 'Wifi', imageUrl: null },
    });

    expect(panel['draftForm'].icon().value()).toBeNull();
    panel['startEdit']();
    await save();
    expect(updateStandardServiceGeneral.mock.calls[0][0].icon).toBe('Wifi');

    panel.open.set(true);
    await new Promise((resolve) => setTimeout(resolve));
    panel['startEdit']();
    panel['chooseIcon'](StandardServiceIcon.Pool);
    await save();
    expect(updateStandardServiceGeneral.mock.calls[1][0].icon).toBe(StandardServiceIcon.Pool);
  });

  it('should send back what the booking options have and the form has not', async () => {
    const { fixture, panel, save, updateStandardServiceBooking } = await setup();

    await panel['requestTab'](2);
    await fixture.whenStable();
    panel['startEdit']();
    panel['draftForm'].bookingOptions[0].maxBookingPerHourQuantity().value.set(25);
    await save();

    expect(updateStandardServiceBooking.mock.calls[0][0].bookingOptions).toMatchObject([
      { ...BOOKING_OPTION, maxBookingPerHourQuantity: 25 },
    ]);
  });

  it('should send back the prices of the periods that are not of the season any more', async () => {
    const { fixture, panel, save, updateStandardServicePrices } = await setup({
      prices: {
        isPeriodPricingEnabled: true,
        periodPrices: [
          { idPeriod: 7, price: 9 },
          { idPeriod: 1, price: 4 },
        ],
      },
      periods: [Object.assign(new SeasonPeriodItem(), { key: 1, name: 'Agosto' })],
    });

    await panel['requestTab'](1);
    await fixture.whenStable();
    panel['startEdit']();
    expect(panel['draft']().periodPrices).toMatchObject([{ idPeriod: 1, price: 4 }]);
    panel['draftForm'].periodPrices[0].price().value.set(5);
    await save();

    expect(updateStandardServicePrices.mock.lastCall?.[0].periodPrices).toMatchObject([
      { idPeriod: 1, price: 5 },
      { idPeriod: 7, price: 9 },
    ]);
  });

  it('should keep Salva off without a price, only in the Prezzi tab', async () => {
    const { fixture, panel } = await setup();

    panel['startEdit']();
    expect(panel['isSaveDisabled']()).toBe(false);
    panel['cancelEdit']();
    await panel['requestTab'](1);
    await fixture.whenStable();
    panel['startEdit']();
    expect(panel['isSaveDisabled']()).toBe(false);
    panel['draftForm'].price().value.set(null);
    expect(panel['isSaveDisabled']()).toBe(true);
  });
});
