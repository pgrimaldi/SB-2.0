import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { AutomaticPriceAddItemRequest } from '../../../../../entities/settings/services/automatic-price-add-item-request';
import { AutomaticPriceServices } from '../../../../../entities/settings/services/automatic-price-services';
import { AutomaticPriceTimeSlot } from '../../../../../entities/settings/services/automatic-price-time-slot';
import { AutomaticPriceValidityPeriod } from '../../../../../entities/settings/services/automatic-price-validity-period';
import { Language } from '../../../../../entities/shared/language';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { AutomaticPriceAddItemPanel } from './automatic-price-add-item-panel';

const IMAGE = new File(['image'], 'coperto.png', { type: 'image/png' });

describe('AutomaticPriceAddItemPanel', () => {
  const setup = async ({
    createAutomaticPrice = vi.fn((request: AutomaticPriceAddItemRequest): Observable<string> =>
      of(request ? 's9' : ''),
    ),
    uploadAutomaticPriceImage = vi.fn((): Observable<void> => of(undefined)),
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
        {
          provide: CatalogService,
          useValue: {
            createAutomaticPrice,
            uploadAutomaticPriceImage,
            automaticPriceServices: () => of(new AutomaticPriceServices()),
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
    const fixture = TestBed.createComponent(AutomaticPriceAddItemPanel);
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
      form.translations[0].serviceName().value.set('Coperto');
      form.translations[0].description().value.set('A persona');
      form.price().value.set(10);
      form.isAllServicesEnabled().value.set(true);
      await fixture.whenStable();
    };
    return { fixture, panel, form, fill, save, createAutomaticPrice, created: () => created };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should want the name in every language that has a description', async () => {
    const { panel, form, fill } = await setup();

    await fill();
    form.translations[1].description().value.set('Per person');
    expect(panel['isSaveDisabled']()).toBe(true);
    expect(form.translations[1].description().errors()).toHaveLength(1);
    form.translations[1].serviceName().value.set('Cover charge');
    expect(panel['isSaveDisabled']()).toBe(false);
    form.translations[1].description().value.set('');
    form.translations[1].serviceName().value.set('');
    expect(panel['isSaveDisabled']()).toBe(false);
  });

  it('should want a service, unless the price applies to all of them, and send only those in force', async () => {
    const { panel, form, fill, save, createAutomaticPrice } = await setup({
      createAutomaticPrice: vi.fn(() =>
        throwError(() => new HttpErrorResponse({ status: 500, error: null })),
      ),
    });

    await fill();
    form.isAllServicesEnabled().value.set(false);
    expect(panel['isSaveDisabled']()).toBe(true);
    form.idHourlyServices().value.set(['h1']);
    expect(panel['isSaveDisabled']()).toBe(false);
    await save();
    expect(createAutomaticPrice.mock.lastCall?.[0]).toMatchObject({
      isAllServicesEnabled: false,
      idHourlyServices: ['h1'],
    });

    form.isAllServicesEnabled().value.set(true);
    await save();
    expect(createAutomaticPrice.mock.lastCall?.[0]).toMatchObject({
      isAllServicesEnabled: true,
      idHourlyServices: [],
    });
  });

  it('should want the price, a day and both hours of every time slot', async () => {
    const { panel, form, fill } = await setup();

    await fill();
    form.price().value.set(null);
    expect(panel['isSaveDisabled']()).toBe(true);
    form.price().value.set(100_001);
    expect(panel['isSaveDisabled']()).toBe(true);
    form.price().value.set(2);
    expect(panel['isSaveDisabled']()).toBe(false);

    form.validityPeriods().value.set([
      Object.assign(new AutomaticPriceValidityPeriod(), {
        timeSlots: [new AutomaticPriceTimeSlot()],
      }),
    ]);
    const timeSlot = form.validityPeriods[0].timeSlots[0];
    expect(panel['isSaveDisabled']()).toBe(true);
    timeSlot.startTime().value.set('12:00');
    expect(panel['isSaveDisabled']()).toBe(true);
    timeSlot.endTime().value.set('15:00');
    expect(panel['isSaveDisabled']()).toBe(false);
    expect(form.validityPeriods[0].daysOfWeek().value()).toEqual([1, 2, 3, 4, 5, 6, 7]);
    form.validityPeriods[0].daysOfWeek().value.set([]);
    expect(panel['isSaveDisabled']()).toBe(true);
  });

  it('should upload the image chosen to the automatic price just created', async () => {
    const uploadAutomaticPriceImage = vi.fn((): Observable<void> => of(undefined));
    const { panel, fill, save, created } = await setup({ uploadAutomaticPriceImage });

    await fill();
    panel['chooseImage'](IMAGE);
    await save();

    expect(uploadAutomaticPriceImage).toHaveBeenCalledExactlyOnceWith('s9', IMAGE);
    expect(panel.open()).toBe(false);
    expect(created()).toBe(1);
    expect(panel['imageError']()).toBeNull();
  });

  it('should close and say it when the automatic price is created but its image is refused', async () => {
    const { panel, fill, save, created } = await setup({
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

    await fill();
    panel['chooseImage'](IMAGE);
    await save();

    expect(panel.open()).toBe(false);
    expect(created()).toBe(1);
    expect(panel['imageError']()?.code).toBe(ApiErrorCode.CatalogInvalidImage);
    expect(document.querySelector('.table__error__popup')).not.toBeNull();
  });
});
