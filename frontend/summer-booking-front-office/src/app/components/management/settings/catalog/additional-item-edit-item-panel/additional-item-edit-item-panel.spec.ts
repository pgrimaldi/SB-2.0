import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, Subject, map, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { AdditionalItemSelectedItem } from '../../../../../entities/settings/catalog/additional-item-selected-item';
import { PriceListService } from '../../../../../services/api/catalog/price-list.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { AdditionalItemEditItemPanel } from './additional-item-edit-item-panel';

describe('AdditionalItemEditItemPanel', () => {
  const setup = async (
    updateAdditionalItem = vi.fn((): Observable<string | null> => of('v2')),
    read = (item: AdditionalItemSelectedItem): Observable<AdditionalItemSelectedItem> => of(item),
  ) => {
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
    const packageCombobox = vi.fn(() => of([{ id: 'k1', value: 'Ombrellone' }]));
    const articleCombobox = vi.fn(() => of([{ id: 'r1', value: 'Lettino' }]));
    TestBed.configureTestingModule({
      providers: [
        provideNativeDateAdapter(),
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
        {
          provide: PriceListService,
          useValue: {
            updateAdditionalItem,
            packageCombobox,
            selectedAdditionalItem: () =>
              read(
                Object.assign(new AdditionalItemSelectedItem(), {
                  idAdditionalItem: 'a1',
                  idArticle: 'r1',
                  idPackages: ['k1', 'k9'],
                  vatRate: 22,
                  version: 'v1',
                }),
              ),
          },
        },
        { provide: WarehouseService, useValue: { articleCombobox } },
        {
          provide: SectorService,
          useValue: { sectorCombobox: () => of([{ id: 's1', value: 'Fila 1' }]) },
        },
        { provide: SeasonService, useValue: { periodList: () => of([]) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      management: {
        settings: {
          catalog: {
            price_list: { calculation_methods: {}, monthly_settings: {}, weekend_settings: {} },
            additional_items: {
              form: {
                missing_article: 'Articolo non più disponibile',
                missing_package: 'Pacchetto non più disponibile',
              },
            },
          },
        },
      },
    });
    translate.use('it');
    const fixture = TestBed.createComponent(AdditionalItemEditItemPanel);
    const panel = fixture.componentInstance;
    fixture.componentRef.setInput('idAdditionalItem', 'a1');
    fixture.componentRef.setInput('open', true);
    await fixture.whenStable();
    const save = async () => {
      panel['startEdit']();
      panel['save']();
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };
    const shown = async (open: boolean) => {
      fixture.componentRef.setInput('open', open);
      await fixture.whenStable();
    };
    return { panel, save, shown, packageCombobox, articleCombobox, updateAdditionalItem };
  };

  const failing = (code: ApiErrorCode, status: number, args?: object) =>
    vi.fn(() => throwError(() => new HttpErrorResponse({ status, error: { status, code, args } })));

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should send back everything read, with its version', async () => {
    const { save, updateAdditionalItem } = await setup();

    await save();
    expect(updateAdditionalItem).toHaveBeenCalledWith(
      'a1',
      expect.objectContaining({ idArticle: 'r1', vatRate: 22 }),
      'v1',
    );
  });

  it('should forget the read of a panel already closed', async () => {
    const reading = new Subject<AdditionalItemSelectedItem>();
    const { panel, shown } = await setup(undefined, () => reading);

    await shown(false);
    reading.error(new HttpErrorResponse({ status: 500 }));
    expect(panel['readError']()).toBeNull();
  });

  it('should show the version conflict only once the item has been read again', async () => {
    const again = new Subject<AdditionalItemSelectedItem>();
    const reads: ((item: AdditionalItemSelectedItem) => Observable<AdditionalItemSelectedItem>)[] =
      [(item) => of(item), (item) => again.pipe(map(() => item))];
    const { panel, save } = await setup(
      failing(ApiErrorCode.ResourceVersionConflict, 409),
      (item) => reads.shift()?.(item) ?? of(item),
    );

    await save();
    expect(panel['isConflictShown']()).toBe(false);
    again.next(new AdditionalItemSelectedItem());
    again.complete();
    expect(panel['isConflictShown']()).toBe(true);
  });

  it('should keep a deleted package among the chosen ones, to be taken away', async () => {
    const { panel, save, packageCombobox } = await setup(
      failing(ApiErrorCode.PricingPackageNotFound, 404),
    );
    expect(panel['packageOptions']()).toEqual([
      { value: 'k1', label: 'Ombrellone' },
      { value: 'k9', label: 'Pacchetto non più disponibile' },
    ]);

    const reads = packageCombobox.mock.calls.length;
    await save();
    expect(packageCombobox.mock.calls).toHaveLength(reads + 1);
    expect(panel['draftForm'].idPackages().errors()).toHaveLength(1);
    expect(panel['error']()).toBeNull();
  });

  it('should forget with Annulla the error of the backend on a field not changed', async () => {
    const { panel, save } = await setup(
      failing(ApiErrorCode.CatalogVatRateNotAllowed, 422, { rates: [4] }),
    );

    await save();
    expect(panel['draftForm'].vatRate().errors()).toHaveLength(1);
    await panel['requestCancel']();
    panel['startEdit']();
    expect(panel['draftForm'].vatRate().errors()).toHaveLength(0);
    expect(panel['isSaveDisabled']()).toBe(false);
    expect(panel['draft']().vatRate).toBe(22);
  });

  it.each([
    [ApiErrorCode.InventoryItemNotFound, 404, 'idArticle'],
    [ApiErrorCode.CatalogVatRateNotAllowed, 422, 'vatRate'],
    [ApiErrorCode.PricingCalculationMethodNotFound, 422, 'calculationMethod'],
  ] as const)('should show %s under its field', async (code, status, field) => {
    const { panel, save } = await setup(failing(code, status, { rates: [4, 22] }));

    await save();
    expect(panel['draftForm'][field]().errors()).toHaveLength(1);
    expect(panel['error']()).toBeNull();
  });
});
