import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { By } from '@angular/platform-browser';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { DiscountScope } from '../../../../../entities/enums/discount-scope';
import { DiscountServiceCategory } from '../../../../../entities/enums/discount-service-category';
import { DiscountType } from '../../../../../entities/enums/discount-type';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { VoucherSelectedItem } from '../../../../../entities/settings/vouchers/voucher-selected-item';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { VoucherService } from '../../../../../services/api/vouchers/voucher.service';
import { VoucherEditItemPanel } from './voucher-edit-item-panel';

const VOUCHER: VoucherSelectedItem = {
  idVoucher: 'v1',
  voucherName: 'Ultimo minuto',
  voucherCode: 'Ultimo minuto',
  isCodeRequired: false,
  validityPeriods: [
    {
      validFrom: '2026-07-01T00:00:00.000Z',
      validTo: '2026-07-31T00:00:00.000Z',
      daysOfWeek: [1, 2],
      timeSlots: [{ from: '09:00', to: '13:00' }],
    },
  ],
  discountType: DiscountType.Percentage,
  discountValue: 12.5,
  discountScope: DiscountScope.Custom,
  serviceCategories: [DiscountServiceCategory.Bar],
  idSectors: ['s1'],
  maxUseQuantity: null,
  isEnabled: true,
  version: 'version-1',
};

@Component({
  imports: [VoucherEditItemPanel],
  template: `<app-voucher-edit-item-panel idVoucher="v1" [(open)]="open" />`,
})
class VoucherEditItemPanelHost {
  readonly open = signal(true);
}

describe('VoucherEditItemPanel', () => {
  const setup = async () => {
    const updateVoucherItem = vi.fn(() => of('version-2'));
    const sectorCombobox = vi.fn(() => of([{ id: 's1', value: 'Fila 1' }]));
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        provideNativeDateAdapter(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        {
          provide: VoucherService,
          useValue: { selectedItem: () => of(VOUCHER), updateVoucherItem },
        },
        { provide: SectorService, useValue: { sectorCombobox } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 1 }) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    const form = { options: { types: {}, scopes: {} }, periods: { days: {} } };
    translate.setTranslation('it', { management: { settings: { vouchers: { form } } } });
    translate.use('it');
    const fixture = TestBed.createComponent(VoucherEditItemPanelHost);
    await fixture.whenStable();
    const panel = fixture.debugElement.query(By.directive(VoucherEditItemPanel))
      .componentInstance as VoucherEditItemPanel;
    const save = async () => {
      panel['save']();
      await fixture.whenStable();
    };
    const refuse = (status: number, code: string, args = {}) =>
      updateVoucherItem.mockReturnValueOnce(
        throwError(() => new HttpErrorResponse({ status, error: { status, code, args } })) as never,
      );
    return { panel, form: panel['draftForm'], save, refuse, updateVoucherItem, sectorCombobox };
  };

  afterEach(() => document.querySelector('.cdk-overlay-container')?.replaceChildren());

  it('should send back everything it read, with the version', async () => {
    const { panel, save, updateVoucherItem } = await setup();
    const { version, ...voucher } = VOUCHER;

    expect(panel.hasUnsavedChanges()).toBe(false);
    await save();

    // As JSON: Signal Forms marks the items of the lists with symbols.
    const sent = updateVoucherItem.mock.lastCall as unknown as [unknown, string];
    expect(JSON.parse(JSON.stringify(sent))).toEqual([voucher, version]);
  });

  it('should want a category or a sector unless the voucher applies to everything, which has none', async () => {
    const { form, save, updateVoucherItem } = await setup();

    form.serviceCategories().value.set([]);
    form.idSectors().value.set([]);
    await save();
    expect(updateVoucherItem).not.toHaveBeenCalled();
    expect(form.serviceCategories().invalid()).toBe(true);

    form.idSectors().value.set(['s1']);
    form.discountScope().value.set(DiscountScope.All);
    await save();
    expect(updateVoucherItem).toHaveBeenCalledWith(
      expect.objectContaining({
        discountScope: DiscountScope.All,
        serviceCategories: [],
        idSectors: [],
      }),
      'version-1',
    );
  });

  it('should want a period only for a discount without a code to type', async () => {
    const { form, save, updateVoucherItem } = await setup();

    form.validityPeriods().value.set([]);
    await save();
    expect(updateVoucherItem).not.toHaveBeenCalled();

    form.isCodeRequired().value.set(true);
    await save();
    expect(updateVoucherItem).toHaveBeenCalledOnce();
  });

  it('should show the refusals of the backend under their fields and read the sectors again', async () => {
    const { panel, form, save, refuse, sectorCombobox } = await setup();

    refuse(409, ApiErrorCode.PricingDiscountCodeTaken, { code: 'ESTATE' });
    await save();
    expect(form.voucherCode().errors()).toHaveLength(1);

    form.voucherCode().value.set('Altro');
    refuse(422, ApiErrorCode.PricingDiscountMaxUsesBelowUsed, { usedCount: 3 });
    await save();
    expect(form.maxUseQuantity().errors()).toHaveLength(1);

    form.maxUseQuantity().value.set(5);
    expect(sectorCombobox).toHaveBeenCalledOnce();
    refuse(404, ApiErrorCode.CatalogSectorNotFound);
    await save();
    expect(sectorCombobox).toHaveBeenCalledTimes(2);
    expect(panel['error']()?.code).toBe(ApiErrorCode.CatalogSectorNotFound);
  });
});
