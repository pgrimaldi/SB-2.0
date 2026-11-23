import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { PriceListService } from '../../../../../services/api/catalog/price-list.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { AdditionalItemAddItemPanel } from './additional-item-add-item-panel';

describe('AdditionalItemAddItemPanel', () => {
  const setup = async (sectorCombobox: () => Observable<unknown>) => {
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
        { provide: PriceListService, useValue: { packageCombobox: () => of([]) } },
        {
          provide: WarehouseService,
          useValue: { articleCombobox: () => of([{ id: 'r1', value: 'Lettino' }]) },
        },
        { provide: SectorService, useValue: { sectorCombobox } },
        { provide: SeasonService, useValue: { periodList: () => of([]) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      management: {
        settings: {
          catalog: {
            price_list: { calculation_methods: {}, monthly_settings: {}, weekend_settings: {} },
          },
        },
      },
    });
    translate.use('it');
    const fixture = TestBed.createComponent(AdditionalItemAddItemPanel);
    fixture.componentRef.setInput('open', true);
    await fixture.whenStable();
    const panel = fixture.componentInstance;
    panel['draftForm'].idArticle().value.set('r1');
    return panel;
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should let the item be created only once its price rows are read', async () => {
    const panel = await setup(() => of([{ id: 's1', value: 'Fila 1' }]));

    expect(panel['isSaveDisabled']()).toBe(false);
  });

  it('should keep Crea off when the price rows cannot be read', async () => {
    const panel = await setup(() => throwError(() => new HttpErrorResponse({ status: 500 })));

    expect(panel['isSaveDisabled']()).toBe(true);
    expect(panel['error']()).not.toBeNull();
  });
});
