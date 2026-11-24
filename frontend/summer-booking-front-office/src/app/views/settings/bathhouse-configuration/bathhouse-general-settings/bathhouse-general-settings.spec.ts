import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { BathhouseConfigurationCacheBehaviour } from '../../../../behaviours/settings/bathhouse-configuration-cache.behaviour';
import { ApiErrorCode } from '../../../../entities/errors/api-error-codes';
import { BathhouseSelectedGeneralInformation } from '../../../../entities/settings/bathhouse-configuration/bathhouse-selected-general-information';
import { Country } from '../../../../entities/shared/country';
import { CatalogService } from '../../../../services/api/catalog/catalog.service';
import { BathhouseGeneralSettings } from './bathhouse-general-settings';

describe('BathhouseGeneralSettings', () => {
  const setup = async (
    saveBathhouseConfigurationGeneral = vi.fn((): Observable<string | null> => of('v2')),
  ) => {
    const information = new BathhouseSelectedGeneralInformation();
    information.bathhouseName = 'Lido Demo';
    information.country = 'IT';
    information.version = 'v1';
    const country = new Country();
    country.code = 'IT';
    country.name = 'Italia';
    const generalInformation = vi.fn(() => of(information));
    const forgetGeneralInformation = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        provideNativeDateAdapter(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        {
          provide: BathhouseConfigurationCacheBehaviour,
          useValue: {
            generalInformation,
            countries: () => of([country]),
            keepGeneralInformation: vi.fn(),
            forgetGeneralInformation,
          },
        },
        { provide: CatalogService, useValue: { saveBathhouseConfigurationGeneral } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      management: {
        settings: {
          bathhouse_configuration: {
            general_information: {
              cancellation_policies: {
                free_until_48_hours: '48 ore',
                free_until_24_hours: '24 ore',
                none: 'Nessuna',
              },
              pets_options: { allowed: 'Sì', not_allowed: 'No', on_request: 'Su richiesta' },
            },
          },
        },
      },
    });
    translate.use('it');
    const fixture = TestBed.createComponent(BathhouseGeneralSettings);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const actions = () => [
      ...element.querySelectorAll<HTMLButtonElement>(
        '.bathhouse__general__settings__actions button',
      ),
    ];
    actions()[0].click();
    await fixture.whenStable();
    const bathhouseName = () => element.querySelector('input')!;
    const type = async (text: string) => {
      bathhouseName().value = text;
      bathhouseName().dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };
    return {
      fixture,
      element,
      actions,
      bathhouseName,
      type,
      generalInformation,
      forgetGeneralInformation,
      saveBathhouseConfigurationGeneral,
    };
  };

  afterEach(() => document.querySelector('.cdk-overlay-container')?.replaceChildren());

  it('should count as a change only a value different from the one read, spaces at the sides aside', async () => {
    const { fixture, type } = await setup();

    await type('Lido Nuovo');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
    await type(' Lido Demo ');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
  });

  it('should read the information again and say why when someone changed it after the read', async () => {
    const save = vi.fn((): Observable<string | null> =>
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { status: 409, code: ApiErrorCode.ResourceVersionConflict },
          }),
      ),
    );
    const {
      fixture,
      element,
      actions,
      bathhouseName,
      type,
      generalInformation,
      forgetGeneralInformation,
    } = await setup(save);
    await type('Lido Nuovo');

    actions()[1].click();
    await fixture.whenStable();
    expect(save).toHaveBeenCalledWith(
      expect.objectContaining({ bathhouseName: 'Lido Nuovo' }),
      'v1',
    );
    expect(forgetGeneralInformation).toHaveBeenCalled();
    expect(generalInformation).toHaveBeenCalledTimes(2);
    expect(bathhouseName().value).toBe('Lido Demo');
    expect(document.querySelector('.table__error__popup')).not.toBeNull();
    expect(element.querySelector('.bathhouse__general__settings__error')).toBeNull();
  });
});
