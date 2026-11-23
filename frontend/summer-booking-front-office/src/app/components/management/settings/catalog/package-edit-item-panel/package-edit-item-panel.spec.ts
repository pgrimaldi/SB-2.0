import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { InventoryArticle } from '../../../../../entities/enums/inventory-article';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { PackageArticle } from '../../../../../entities/settings/catalog/package-article';
import { PackageSelectedItem } from '../../../../../entities/settings/catalog/package-selected-item';
import { Language } from '../../../../../entities/shared/language';
import { PriceListService } from '../../../../../services/api/catalog/price-list.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { PackageEditItemPanel } from './package-edit-item-panel';

describe('PackageEditItemPanel', () => {
  const setup = async ({
    updatePackage = vi.fn((): Observable<string | null> => of('v2')),
    selected = {} as Partial<PackageSelectedItem>,
    articles = [] as PackageArticle[],
  } = {}) => {
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
    const sectorCombobox = vi.fn(() => of([{ id: 's1', value: 'Fila 1' }]));
    TestBed.configureTestingModule({
      providers: [
        provideNativeDateAdapter(),
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
        { provide: LanguageBehaviour, useValue: { current: signal(Language.It) } },
        {
          provide: PriceListService,
          useValue: {
            updatePackage,
            packageArticles: () => of(articles),
            selectedPackage: () =>
              of(
                Object.assign(new PackageSelectedItem(), {
                  idPackage: 'k1',
                  translations: [{ language: Language.It, packageName: 'Ombrellone' }],
                  version: 'v1',
                  ...selected,
                }),
              ),
          },
        },
        { provide: SectorService, useValue: { sectorCombobox, sectorAreaCombobox: () => of([]) } },
        { provide: SeasonService, useValue: { periodList: () => of([]) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      management: {
        settings: {
          catalog: {
            packages: {
              form: {
                tabs: { general_info: 'Info Generali', price_list: 'Listino' },
                missing_sector: 'Settore/fila non più disponibile',
              },
            },
          },
        },
      },
    });
    translate.use('it');
    const fixture = TestBed.createComponent(PackageEditItemPanel);
    const panel = fixture.componentInstance;
    fixture.componentRef.setInput('idPackage', 'k1');
    fixture.componentRef.setInput('open', true);
    await fixture.whenStable();
    const save = async () => {
      panel['save']();
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };
    return { panel, save, sectorCombobox, updatePackage };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should keep a deleted sector among the chosen ones, to be taken away', async () => {
    const { panel, save, sectorCombobox } = await setup({
      selected: { idSectors: ['s1', 's9'] },
      updatePackage: vi.fn(() =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 404,
              error: { status: 404, code: ApiErrorCode.CatalogSectorNotFound },
            }),
        ),
      ),
    });

    panel['startEdit']();
    const reads = sectorCombobox.mock.calls.length;
    await save();
    expect(panel['error']()?.code).toBe(ApiErrorCode.CatalogSectorNotFound);
    expect(panel['draft']().idSectors).toEqual(['s1', 's9']);
    expect(sectorCombobox.mock.calls).toHaveLength(reads + 1);
    expect(panel['isSaveDisabled']()).toBe(false);
  });

  it('should send back the items of the package that have no row', async () => {
    const { panel, save, updatePackage } = await setup({
      selected: {
        items: [
          { idInventoryItem: 'table', quantity: 1 },
          { idInventoryItem: 'sunbed', quantity: 2 },
          { idInventoryItem: 'umbrella', quantity: 1 },
        ],
        vatRate: 22,
      },
      articles: [
        {
          article: InventoryArticle.Umbrella,
          items: [{ idInventoryItem: 'umbrella', itemName: 'Ombrellone', totalQuantity: 60 }],
        },
        {
          article: InventoryArticle.Sunbed,
          items: [{ idInventoryItem: 'sunbed', itemName: 'Lettino', totalQuantity: 120 }],
        },
        { article: InventoryArticle.Gazebo, items: [] },
      ],
    });

    expect(panel.hasUnsavedChanges()).toBe(false);
    panel['startEdit']();
    await save();
    expect(updatePackage.mock.calls).toMatchObject([
      [
        'k1',
        {
          items: [
            { idInventoryItem: 'umbrella', quantity: 1 },
            { idInventoryItem: 'sunbed', quantity: 2 },
            { idInventoryItem: 'table', quantity: 1 },
          ],
          vatRate: 22,
        },
        'v1',
      ],
    ]);
  });
});
