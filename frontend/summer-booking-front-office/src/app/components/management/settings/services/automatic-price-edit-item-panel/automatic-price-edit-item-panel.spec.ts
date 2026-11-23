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
import { AutomaticPriceSelectedGeneralInfo } from '../../../../../entities/settings/services/automatic-price-selected-general-info';
import { AutomaticPriceSelectedPricesValidity } from '../../../../../entities/settings/services/automatic-price-selected-prices-validity';
import { AutomaticPriceServices } from '../../../../../entities/settings/services/automatic-price-services';
import { AutomaticPriceUpdatePricesValidityRequest } from '../../../../../entities/settings/services/automatic-price-update-prices-validity-request';
import { AutomaticPriceUpdateGeneralRequest } from '../../../../../entities/settings/services/automatic-price-update-general-request';
import { Language } from '../../../../../entities/shared/language';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { AutomaticPriceEditItemPanel } from './automatic-price-edit-item-panel';

const IMAGE = new File(['image'], 'coperto.png', { type: 'image/png' });
const SAVED_IMAGE_URL = 'https://files.example.test/coperto.webp';
describe('AutomaticPriceEditItemPanel', () => {
  const setup = async ({
    uploadAutomaticPriceImage = vi.fn((): Observable<void> => of(undefined)),
    deleteAutomaticPriceImage = vi.fn((): Observable<void> => of(undefined)),
    generalInfo = {} as Partial<AutomaticPriceSelectedGeneralInfo>,
    pricesValidity = {} as Partial<AutomaticPriceSelectedPricesValidity>,
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
    const updateAutomaticPriceGeneral = vi.fn(
      (request: AutomaticPriceUpdateGeneralRequest): Observable<string | null> =>
        of(request ? 'v2' : null),
    );
    const updateAutomaticPricePricesValidity = vi.fn(
      (request: AutomaticPriceUpdatePricesValidityRequest): Observable<string | null> =>
        of(request ? 'v2' : null),
    );
    const automaticPriceServices = vi.fn((): Observable<AutomaticPriceServices> =>
      of({
        barServices: [
          { id: 'b1', value: 'Caffè' },
          { id: 'b2', value: 'Pizza' },
        ],
        standardServices: [],
        hourlyServices: [],
      }),
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
            uploadAutomaticPriceImage,
            deleteAutomaticPriceImage,
            updateAutomaticPriceGeneral,
            updateAutomaticPricePricesValidity,
            automaticPriceServices,
            automaticPricePricesValidity: () =>
              of(
                Object.assign(new AutomaticPriceSelectedPricesValidity(), {
                  price: 2,
                  idBarServices: ['b2', 'b1'],
                  version: 'v1',
                  ...pricesValidity,
                }),
              ),
            automaticPriceGeneralInfo: () =>
              of(
                Object.assign(new AutomaticPriceSelectedGeneralInfo(), {
                  idAutomaticPrice: 's5',
                  translations: [
                    { language: Language.It, serviceName: 'Coperto', description: 'A persona' },
                  ],
                  imageUrl: SAVED_IMAGE_URL,
                  version: 'v1',
                  ...generalInfo,
                }),
              ),
          },
        },
        { provide: LanguageBehaviour, useValue: { current: signal(Language.It) } },
        { provide: SectorService, useValue: { sectorCombobox: () => of([]) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      management: {
        settings: {
          services: {
            form: {
              tabs: { general_info: 'Info Generali', prices_validity: 'Prezzi e Validità' },
              bookings: { time_intervals: {} },
              availability: { days: {}, time_slot_names: {} },
            },
            hourly_services: { categories: {} },
          },
        },
      },
    });
    translate.use('it');
    const fixture = TestBed.createComponent(AutomaticPriceEditItemPanel);
    const panel = fixture.componentInstance;
    let updated = 0;
    panel.updated.subscribe(() => updated++);
    fixture.componentRef.setInput('idAutomaticPrice', 's5');
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
      updateAutomaticPriceGeneral,
      updateAutomaticPricePricesValidity,
      automaticPriceServices,
      updated: () => updated,
    };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should save the Prezzi e Validità tab alone, with the services in force and its version', async () => {
    const { fixture, panel, save, updateAutomaticPricePricesValidity, updated } = await setup({
      pricesValidity: { vatRate: 10, idSectors: ['s1'], version: 'v7' },
    });

    await panel['requestTab'](1);
    await fixture.whenStable();
    panel['startEdit']();
    panel['draftForm'].price().value.set(3);
    panel['draftForm'].isAllServicesEnabled().value.set(true);
    await save();

    expect(updateAutomaticPricePricesValidity).toHaveBeenCalledExactlyOnceWith(
      Object.assign(new AutomaticPriceUpdatePricesValidityRequest(), {
        idAutomaticPrice: 's5',
        price: 3,
        vatRate: 10,
        isAllServicesEnabled: true,
        idSectors: ['s1'],
      }),
      'v7',
    );
    expect(panel.open()).toBe(false);
    expect(updated()).toBe(1);
  });

  it('should take away a chosen service that is no longer there, reading them again', async () => {
    const { fixture, panel, save, updateAutomaticPricePricesValidity, automaticPriceServices } =
      await setup();
    updateAutomaticPricePricesValidity.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 404,
            error: { status: 404, code: ApiErrorCode.ServiceNotFound },
          }),
      ),
    );

    await panel['requestTab'](1);
    await fixture.whenStable();
    panel['startEdit']();
    panel['draftForm'].price().value.set(3);
    automaticPriceServices.mockReturnValue(
      of({
        barServices: [{ id: 'b1', value: 'Caffè' }],
        standardServices: [],
        hourlyServices: [],
      }),
    );
    await save();
    expect(panel['error']()?.code).toBe(ApiErrorCode.ServiceNotFound);
    expect(panel['draft']()).toMatchObject({ price: 3, idBarServices: ['b1'] });
    expect(panel['isSaveDisabled']()).toBe(false);
  });

  it('should not ask for the order of the services, nor for those out of force', async () => {
    const { fixture, panel } = await setup({
      pricesValidity: { isAllServicesEnabled: true, idBarServices: [], idSectors: ['s2', 's1'] },
    });

    await panel['requestTab'](1);
    await fixture.whenStable();
    panel['startEdit']();
    panel['draftForm'].isAllServicesEnabled().value.set(false);
    panel['draftForm'].idBarServices().value.set(['b1']);
    expect(panel.hasUnsavedChanges()).toBe(true);
    panel['draftForm'].isAllServicesEnabled().value.set(true);
    panel['draftForm'].idSectors().value.set(['s1', 's2']);
    expect(panel.hasUnsavedChanges()).toBe(false);
  });

  it('should show at once, in Modifica, what was read already wrong', async () => {
    const { fixture, panel } = await setup({ pricesValidity: { idBarServices: [] } });

    await panel['requestTab'](1);
    await fixture.whenStable();
    const { touched, errors } = panel['draftForm'].isAllServicesEnabled();
    expect(touched()).toBe(false);
    panel['startEdit']();
    expect(panel['isSaveDisabled']()).toBe(true);
    expect(touched()).toBe(true);
    expect(errors()).toHaveLength(1);
  });

  it('should show the image of the automatic price until an icon or another image is chosen', async () => {
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
    const { panel, save, updateAutomaticPriceGeneral } = await setup({
      generalInfo: { icon: StandardServiceIcon.Shower, imageUrl: null },
    });

    panel['startEdit']();
    panel['chooseImage'](IMAGE);
    expect(panel['draftForm'].icon().value()).toBeNull();
    await save();
    expect(updateAutomaticPriceGeneral.mock.calls[0][0].icon).toBe(StandardServiceIcon.Shower);
  });

  it('should take away an image not saved yet without asking, back to the icon read', async () => {
    const deleteAutomaticPriceImage = vi.fn((): Observable<void> => of(undefined));
    const { panel } = await setup({
      deleteAutomaticPriceImage,
      generalInfo: { icon: StandardServiceIcon.Shower, imageUrl: null },
    });

    panel['startEdit']();
    panel['chooseImage'](IMAGE);
    panel['removeImage']();
    expect(panel['imagePreviewUrl']()).toBeNull();
    expect(panel['draftForm'].icon().value()).toBe(StandardServiceIcon.Shower);
    expect(panel.hasUnsavedChanges()).toBe(false);
    expect(panel['isImageDeletionAsked']()).toBe(false);
    expect(deleteAutomaticPriceImage).not.toHaveBeenCalled();
  });

  it('should delete the saved image only once confirmed, only in Modifica', async () => {
    const deleteAutomaticPriceImage = vi.fn((): Observable<void> => of(undefined));
    const { panel } = await setup({ deleteAutomaticPriceImage });

    panel['removeImage']();
    expect(panel['isImageDeletionAsked']()).toBe(false);
    panel['startEdit']();
    panel['removeImage']();
    expect(panel['isImageDeletionAsked']()).toBe(true);
    expect(deleteAutomaticPriceImage).not.toHaveBeenCalled();

    panel['confirmImageDeletion']();
    expect(deleteAutomaticPriceImage).toHaveBeenCalledExactlyOnceWith('s5');
    expect(panel['imagePreviewUrl']()).toBeNull();
    expect(panel['isEditing']()).toBe(true);
  });

  it('should keep the image and say why when it cannot be deleted', async () => {
    const { fixture, panel } = await setup({
      deleteAutomaticPriceImage: vi.fn(() =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 404,
              error: { status: 404, code: ApiErrorCode.ServiceAutomaticPriceNotFound },
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
    expect(failures).toEqual([ApiErrorCode.ServiceAutomaticPriceNotFound]);
  });

  it('should upload the image chosen after the data of its tab', async () => {
    const uploadAutomaticPriceImage = vi.fn((): Observable<void> => of(undefined));
    const { panel, save, updateAutomaticPriceGeneral, updated } = await setup({
      uploadAutomaticPriceImage,
    });

    panel['startEdit']();
    panel['chooseImage'](IMAGE);
    await save();

    expect(updateAutomaticPriceGeneral).toHaveBeenCalledTimes(1);
    expect(uploadAutomaticPriceImage).toHaveBeenCalledExactlyOnceWith('s5', IMAGE);
    expect(updateAutomaticPriceGeneral.mock.invocationCallOrder[0]).toBeLessThan(
      uploadAutomaticPriceImage.mock.invocationCallOrder[0],
    );
    expect(panel.open()).toBe(false);
    expect(updated()).toBe(1);
  });

  it('should close and say it when the data are saved but the image is refused', async () => {
    const { panel, save, updated } = await setup({
      uploadAutomaticPriceImage: vi.fn(() =>
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
    const { panel, save, updateAutomaticPriceGeneral } = await setup({
      generalInfo: { icon: 'Wifi', imageUrl: null },
    });

    expect(panel['draftForm'].icon().value()).toBeNull();
    panel['startEdit']();
    await save();
    expect(updateAutomaticPriceGeneral.mock.calls[0][0].icon).toBe('Wifi');

    panel.open.set(true);
    await new Promise((resolve) => setTimeout(resolve));
    panel['startEdit']();
    panel['chooseIcon'](StandardServiceIcon.Pool);
    await save();
    expect(updateAutomaticPriceGeneral.mock.calls[1][0].icon).toBe(StandardServiceIcon.Pool);
  });
});
