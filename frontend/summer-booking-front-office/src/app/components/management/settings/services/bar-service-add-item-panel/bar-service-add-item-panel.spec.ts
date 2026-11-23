import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ServiceVariant } from '../../../../../entities/settings/services/service-variant';
import { Language } from '../../../../../entities/shared/language';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { BarServiceAddItemPanel } from './bar-service-add-item-panel';

const IMAGE = new File(['image'], 'caffe.png', { type: 'image/png' });

describe('BarServiceAddItemPanel', () => {
  const setup = async ({
    createBarService = vi.fn((): Observable<string> => of('b9')),
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
        { provide: CatalogService, useValue: { createBarService, uploadServiceImage } },
        { provide: LanguageBehaviour, useValue: { current: signal(Language.It) } },
        { provide: SeasonService, useValue: { periodList: () => of([]) } },
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
            bar_services: { categories: {} },
          },
        },
      },
    });
    translate.use('it');
    const fixture = TestBed.createComponent(BarServiceAddItemPanel);
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
      form.translations[0].serviceName().value.set('Caffè');
      form.translations[0].description().value.set('Espresso');
      form.price().value.set(1);
      await fixture.whenStable();
    };
    return { fixture, panel, form, fill, save, created: () => created };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should keep Salva off until the price and every variant are filled', async () => {
    const { fixture, panel, form, fill } = await setup();

    await fill();
    expect(panel['isSaveDisabled']()).toBe(false);
    form.price().value.set(null);
    expect(panel['isSaveDisabled']()).toBe(true);
    form.price().value.set(100_001);
    expect(panel['isSaveDisabled']()).toBe(true);
    form.price().value.set(0);
    expect(panel['isSaveDisabled']()).toBe(false);

    form.variants().value.set([new ServiceVariant()]);
    await fixture.whenStable();
    expect(panel['isSaveDisabled']()).toBe(true);
    form.variants[0].variantName().value.set('Doppio');
    expect(panel['isSaveDisabled']()).toBe(true);
    form.variants[0].price().value.set(1.8);
    expect(panel['isSaveDisabled']()).toBe(false);
  });

  it('should want the name in every language that has a description', async () => {
    const { panel, form, fill } = await setup();

    await fill();
    form.translations[1].description().value.set('Espresso in a small cup');
    expect(panel['isSaveDisabled']()).toBe(true);
    expect(form.translations[1].description().errors()).toHaveLength(1);
    form.translations[1].serviceName().value.set('Coffee');
    expect(panel['isSaveDisabled']()).toBe(false);
    form.translations[1].description().value.set('');
    form.translations[1].serviceName().value.set('');
    expect(panel['isSaveDisabled']()).toBe(false);
  });

  it('should upload the image chosen to the service just created', async () => {
    const uploadServiceImage = vi.fn((): Observable<void> => of(undefined));
    const { panel, fill, save, created } = await setup({ uploadServiceImage });

    await fill();
    panel['chooseImage'](IMAGE);
    await save();

    expect(uploadServiceImage).toHaveBeenCalledExactlyOnceWith('b9', IMAGE);
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

  it('should show a VAT rate that is not allowed under its field, in its tab', async () => {
    const { panel, form, fill, save } = await setup({
      createBarService: vi.fn(() =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 422,
              error: {
                status: 422,
                code: ApiErrorCode.CatalogVatRateNotAllowed,
                args: { rates: [4, 10, 22] },
              },
            }),
        ),
      ),
    });

    await fill();
    form.vatRate().value.set(7);
    await save();

    expect(panel.open()).toBe(true);
    expect(panel['selectedTab']()).toBe(1);
    expect(form.vatRate().errors()).toEqual([
      expect.objectContaining({
        code: ApiErrorCode.CatalogVatRateNotAllowed,
        args: { rates: '4, 10, 22' },
      }),
    ]);
    form.vatRate().value.set(10);
    expect(panel['isSaveDisabled']()).toBe(false);
  });
});
