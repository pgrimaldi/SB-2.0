import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, Subject, map, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { InventoryArticle } from '../../../../../entities/enums/inventory-article';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { PackageArticle } from '../../../../../entities/settings/catalog/package-article';
import { PackagePeriodPrice } from '../../../../../entities/settings/catalog/package-period-price';
import { PackageSelectedItem } from '../../../../../entities/settings/catalog/package-selected-item';
import { Language } from '../../../../../entities/shared/language';
import { PriceListService } from '../../../../../services/api/catalog/price-list.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { PanelTab } from '../package-panel-tabs';
import { PackageEditItemPanel } from './package-edit-item-panel';

describe('PackageEditItemPanel', () => {
  const setup = async ({
    updatePackage = vi.fn((): Observable<string | null> => of('v2')),
    selected = {} as Partial<PackageSelectedItem>,
    articles = [] as PackageArticle[],
    read = (item: PackageSelectedItem): Observable<PackageSelectedItem> => of(item),
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
              read(
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
            price_list: { calculation_methods: {}, monthly_settings: {}, weekend_settings: {} },
            packages: {
              form: {
                tabs: { general_info: 'Info Generali', price_list: 'Listino' },
                missing_sector: 'Settore/fila non più disponibile',
                articles: { none: 'Nessuno' },
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
    const shown = async (open: boolean) => {
      fixture.componentRef.setInput('open', open);
      await fixture.whenStable();
    };
    return { panel, save, shown, sectorCombobox, updatePackage };
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

  it('should forget with Annulla the error of the backend on a field not changed', async () => {
    const { panel, save } = await setup({
      selected: { vatRate: 20 },
      updatePackage: vi.fn(() =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 422,
              error: {
                status: 422,
                code: ApiErrorCode.CatalogVatRateNotAllowed,
                args: { rates: [22] },
              },
            }),
        ),
      ),
    });

    panel['startEdit']();
    await save();
    expect(panel['draftForm'].vatRate().errors()).toHaveLength(1);
    panel['cancelEdit']();
    panel['startEdit']();
    expect(panel['draftForm'].vatRate().errors()).toHaveLength(0);
    expect(panel['isSaveDisabled']()).toBe(false);
    expect(panel['draft']().vatRate).toBe(20);
    expect(panel.hasUnsavedChanges()).toBe(false);
  });

  const conflict = () =>
    vi.fn(() =>
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { status: 409, code: ApiErrorCode.ResourceVersionConflict },
          }),
      ),
    );

  it('should read the sectors once per opening', async () => {
    const { panel, shown, sectorCombobox } = await setup();

    panel['selectedTab'].set(PanelTab.PriceList);
    await shown(true);
    panel['selectedTab'].set(PanelTab.GeneralInfo);
    await shown(true);
    expect(sectorCombobox).toHaveBeenCalledOnce();
    await shown(false);
    await shown(true);
    expect(sectorCombobox).toHaveBeenCalledTimes(2);
  });

  it('should forget the read of a panel already closed', async () => {
    const reading = new Subject<PackageSelectedItem>();
    const { panel, shown } = await setup({ read: () => reading });

    await shown(false);
    reading.error(new HttpErrorResponse({ status: 500 }));
    expect(panel['readError']()).toBeNull();
  });

  it('should show the version conflict only once the package has been read again', async () => {
    const again = new Subject<PackageSelectedItem>();
    const reads: ((item: PackageSelectedItem) => Observable<PackageSelectedItem>)[] = [
      (item) => of(item),
      (item) => again.pipe(map(() => item)),
    ];
    const { panel, save } = await setup({
      updatePackage: conflict(),
      read: (item) => reads.shift()?.(item) ?? of(item),
    });

    panel['startEdit']();
    panel['selectedTab'].set(PanelTab.PriceList);
    await save();
    expect(panel['isConflictShown']()).toBe(false);
    again.next(new PackageSelectedItem());
    again.complete();
    expect(panel['isConflictShown']()).toBe(true);
    expect(panel['selectedTab']()).toBe(PanelTab.PriceList);
    expect(panel['isEditing']()).toBe(false);
  });

  it('should show only the read error when the read after a version conflict fails', async () => {
    const reads: ((item: PackageSelectedItem) => Observable<PackageSelectedItem>)[] = [
      (item) => of(item),
      () => throwError(() => new HttpErrorResponse({ status: 500 })),
    ];
    const { panel, save } = await setup({
      updatePackage: conflict(),
      read: (item) => reads.shift()?.(item) ?? of(item),
    });

    panel['startEdit']();
    await save();
    expect(panel['isConflictShown']()).toBe(false);
    expect(panel['readError']()).not.toBeNull();
    expect(panel.open()).toBe(false);
  });

  it('should not count the order of the sectors as a change', async () => {
    const { panel } = await setup({ selected: { idSectors: ['s2', 's1'] } });

    panel['draftForm'].idSectors().value.set(['s1', 's2']);
    expect(panel.hasUnsavedChanges()).toBe(false);
  });

  it('should send back the prices that have no row', async () => {
    const { panel, save, updatePackage } = await setup({
      selected: {
        periodPrices: [
          Object.assign(new PackagePeriodPrice(), { idSector: null, idPeriod: 0, dailyPrice: 9 }),
        ],
      },
    });

    panel['startEdit']();
    await save();
    expect(updatePackage.mock.calls).toMatchObject([
      ['k1', { periodPrices: [{ idSector: null, idPeriod: 0, dailyPrice: 9 }] }, 'v1'],
    ]);
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
