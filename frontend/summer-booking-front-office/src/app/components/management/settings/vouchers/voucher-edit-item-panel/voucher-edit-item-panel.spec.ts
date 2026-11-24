import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { By } from '@angular/platform-browser';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, Subject, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { DiscountScope } from '../../../../../entities/enums/discount-scope';
import { DiscountServiceCategory } from '../../../../../entities/enums/discount-service-category';
import { DiscountType } from '../../../../../entities/enums/discount-type';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { MAX_VOUCHER_USE_QUANTITY } from '../../../../../entities/settings/vouchers/voucher-add-item-request';
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
  template: `<app-voucher-edit-item-panel
    idVoucher="v1"
    [(open)]="open"
    (updated)="updates = updates + 1"
    (failed)="failures.push($event)"
  />`,
})
class VoucherEditItemPanelHost {
  readonly open = signal(true);
  updates = 0;
  readonly failures: ApiProblem[] = [];
}

const refusal = (status: number, code: string, args = {}) =>
  throwError(() => new HttpErrorResponse({ status, error: { status, code, args } }));

describe('VoucherEditItemPanel', () => {
  const setup = async (
    selectedItem = vi.fn((): Observable<VoucherSelectedItem> => of(VOUCHER)),
  ) => {
    const updateVoucherItem = vi.fn(() => of('version-2'));
    const sectorCombobox = vi.fn(() =>
      of([
        { id: 's1', value: 'Fila 1' },
        { id: 's2', value: 'Fila 2' },
      ]),
    );
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        provideNativeDateAdapter(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        {
          provide: VoucherService,
          useValue: { selectedItem, updateVoucherItem },
        },
        { provide: SectorService, useValue: { sectorCombobox } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 1 }) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    const form = { options: { scopes: {} }, periods: { days: {} } };
    translate.setTranslation('it', {
      management: { settings: { vouchers: { form, discount_type: {} } } },
    });
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
      updateVoucherItem.mockReturnValueOnce(refusal(status, code, args) as never);
    return {
      fixture,
      host: fixture.componentInstance,
      panel,
      form: panel['draftForm'],
      save,
      refuse,
      selectedItem,
      updateVoucherItem,
      sectorCombobox,
    };
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

  it('should save nothing until the voucher is read and forget an answer that comes after the closing', async () => {
    const answer = new Subject<VoucherSelectedItem>();
    const { fixture, host, panel, save, updateVoucherItem } = await setup(vi.fn(() => answer));

    await save();
    expect(updateVoucherItem).not.toHaveBeenCalled();

    host.open.set(false);
    await fixture.whenStable();
    answer.next(VOUCHER);
    expect(panel['isLoaded']()).toBe(false);
  });

  it('should close, tell the page and offer to read again when the voucher cannot be read', async () => {
    const { fixture, host, panel, selectedItem } = await setup(
      vi
        .fn((): Observable<VoucherSelectedItem> => of(VOUCHER))
        .mockReturnValueOnce(refusal(404, ApiErrorCode.PricingDiscountNotFound)),
    );

    expect(host.open()).toBe(false);
    expect(host.failures.map(({ code }) => code)).toEqual([ApiErrorCode.PricingDiscountNotFound]);
    expect(panel['readError']()?.code).toBe(ApiErrorCode.PricingDiscountNotFound);

    panel['retryRead']();
    await fixture.whenStable();
    expect(selectedItem).toHaveBeenCalledTimes(2);
    expect(panel['isLoaded']()).toBe(true);
  });

  it('should close with the alert when another user changed the voucher, and stay open when it is gone', async () => {
    const { fixture, host, panel, save, refuse } = await setup();

    refuse(404, ApiErrorCode.PricingDiscountNotFound);
    await save();
    expect(host.open()).toBe(true);
    expect(panel['error']()?.code).toBe(ApiErrorCode.PricingDiscountNotFound);

    refuse(409, ApiErrorCode.ResourceVersionConflict);
    await save();
    await fixture.whenStable();
    expect(host.open()).toBe(false);
    expect(panel['isConflictShown']()).toBe(true);
    expect(host.failures.map(({ code }) => code)).toEqual([
      ApiErrorCode.PricingDiscountNotFound,
      ApiErrorCode.ResourceVersionConflict,
    ]);
    expect(host.updates).toBe(0);
  });

  it('should say above the buttons, without blocking the next save, an error on a field it does not show', async () => {
    const { panel, save, updateVoucherItem } = await setup();
    const error = {
      status: 400,
      code: ApiErrorCode.ValidationFailed,
      errors: [{ field: 'isCodeRequired', code: ApiErrorCode.ValidationInvalid }],
    };
    updateVoucherItem.mockReturnValueOnce(
      throwError(() => new HttpErrorResponse({ status: error.status, error })) as never,
    );

    await save();
    expect(panel['error']()?.code).toBe(ApiErrorCode.ValidationInvalid);

    await save();
    expect(updateVoucherItem).toHaveBeenCalledTimes(2);
  });

  it('should not count another order of the categories and of the sectors as a change', async () => {
    const { form, panel } = await setup(
      vi.fn(() =>
        of({
          ...VOUCHER,
          serviceCategories: [DiscountServiceCategory.Additional, DiscountServiceCategory.Bar],
          idSectors: ['s2', 's1'],
        }),
      ),
    );

    form
      .serviceCategories()
      .value.set([DiscountServiceCategory.Bar, DiscountServiceCategory.Additional]);
    form.idSectors().value.set(['s1', 's2']);
    expect(panel.hasUnsavedChanges()).toBe(false);

    form.idSectors().value.set(['s1']);
    expect(panel.hasUnsavedChanges()).toBe(true);
  });

  it('should want both times of a time slot and no more uses than the backend keeps', async () => {
    const { form, save, updateVoucherItem } = await setup();

    form.validityPeriods[0].timeSlots[0].to().value.set(null);
    await save();
    expect(updateVoucherItem).not.toHaveBeenCalled();

    form.validityPeriods[0].timeSlots[0].to().value.set('13:00');
    form.maxUseQuantity().value.set(MAX_VOUCHER_USE_QUANTITY + 1);
    await save();
    expect(updateVoucherItem).not.toHaveBeenCalled();

    form.maxUseQuantity().value.set(MAX_VOUCHER_USE_QUANTITY);
    await save();
    expect(updateVoucherItem).toHaveBeenCalledOnce();
  });
});
