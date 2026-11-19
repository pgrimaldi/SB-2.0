import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { BathhouseConfigurationCacheBehaviour } from '../../../../behaviours/settings/bathhouse-configuration-cache.behaviour';
import { BathhouseGeneralInformation } from '../../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { Country } from '../../../../entities/shared/country';
import { CatalogService } from '../../../../services/api/catalog/catalog.service';
import { BathhouseGeneralSettings } from './bathhouse-general-settings';

describe('BathhouseGeneralSettings', () => {
  it('should count as a change only a value different from the one read, spaces at the sides aside', async () => {
    const information = new BathhouseGeneralInformation();
    information.bathhouseName = 'Lido Demo';
    information.country = 'IT';
    const country = new Country();
    country.code = 'IT';
    country.name = 'Italia';
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        provideNativeDateAdapter(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        {
          provide: BathhouseConfigurationCacheBehaviour,
          useValue: {
            generalInformation: () => of(information),
            countries: () => of([country]),
            keepGeneralInformation: vi.fn(),
          },
        },
        { provide: CatalogService, useValue: { saveBathhouseConfigurationGeneral: vi.fn() } },
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
    element
      .querySelector<HTMLButtonElement>('.bathhouse__general__settings__actions button')!
      .click();
    await fixture.whenStable();
    const bathhouseName = element.querySelector('input')!;
    const type = async (text: string) => {
      bathhouseName.value = text;
      bathhouseName.dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };

    await type('Lido Nuovo');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
    await type(' Lido Demo ');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
  });
});
