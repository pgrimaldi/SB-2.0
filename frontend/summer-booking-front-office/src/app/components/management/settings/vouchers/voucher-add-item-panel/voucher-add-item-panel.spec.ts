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
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { VoucherAddItemRequest } from '../../../../../entities/settings/vouchers/voucher-add-item-request';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { VoucherService } from '../../../../../services/api/vouchers/voucher.service';
import { VoucherAddItemPanel } from './voucher-add-item-panel';

@Component({
  imports: [VoucherAddItemPanel],
  template: `<app-voucher-add-item-panel [(open)]="open" (added)="additions = additions + 1" />`,
})
class VoucherAddItemPanelHost {
  readonly open = signal(true);
  additions = 0;
}

describe('VoucherAddItemPanel', () => {
  const setup = async () => {
    const addVoucherItem = vi.fn((): Observable<void> => of(undefined));
    const sectorCombobox = vi.fn(() => of([{ id: 's1', value: 'Fila 1' }]));
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        provideNativeDateAdapter(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: VoucherService, useValue: { addVoucherItem } },
        { provide: SectorService, useValue: { sectorCombobox } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 1 }) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    const texts = { options: { scopes: {} }, periods: { days: {} } };
    translate.setTranslation('it', {
      management: { settings: { vouchers: { form: texts, discount_type: {} } } },
    });
    translate.use('it');
    const fixture = TestBed.createComponent(VoucherAddItemPanelHost);
    await fixture.whenStable();
    const panel = fixture.debugElement.query(By.directive(VoucherAddItemPanel))
      .componentInstance as VoucherAddItemPanel;
    const form = panel['draftForm'];
    const fill = () => {
      form.voucherName().value.set('Estate');
      form.voucherCode().value.set('ESTATE');
      form.discountValue().value.set(15);
    };
    const save = async () => {
      panel['save']();
      await fixture.whenStable();
    };
    return {
      fixture,
      host: fixture.componentInstance,
      panel,
      form,
      fill,
      save,
      addVoucherItem,
      sectorCombobox,
    };
  };

  afterEach(() => document.querySelector('.cdk-overlay-container')?.replaceChildren());

  it('should add the voucher once, then close, tell the page and open empty again', async () => {
    const { fixture, host, panel, form, fill, save, addVoucherItem } = await setup();
    const answer = new Subject<void>();

    await save();
    expect(addVoucherItem).not.toHaveBeenCalled();

    fill();
    expect(panel.hasUnsavedChanges()).toBe(true);
    addVoucherItem.mockReturnValueOnce(answer);
    await save();
    await save();
    expect(addVoucherItem).toHaveBeenCalledExactlyOnceWith(
      Object.assign(new VoucherAddItemRequest(), {
        voucherName: 'Estate',
        voucherCode: 'ESTATE',
        discountValue: 15,
      }),
    );
    expect(host.open()).toBe(true);

    answer.next();
    await fixture.whenStable();
    expect(host.open()).toBe(false);
    expect(host.additions).toBe(1);

    await fixture.whenStable();
    host.open.set(true);
    await fixture.whenStable();
    expect(form().value()).toEqual({ ...new VoucherAddItemRequest() });
    expect(panel.hasUnsavedChanges()).toBe(false);
  });

  it('should show a code already taken under its field until the code changes', async () => {
    const { host, form, fill, save, addVoucherItem } = await setup();
    const status = 409;
    const code = ApiErrorCode.PricingDiscountCodeTaken;
    addVoucherItem.mockReturnValueOnce(
      throwError(
        () => new HttpErrorResponse({ status, error: { status, code, args: { code: 'ESTATE' } } }),
      ),
    );

    fill();
    await save();
    expect(form.voucherCode().errors()).toHaveLength(1);
    expect(host.open()).toBe(true);

    form.voucherCode().value.set('ESTATE2');
    await save();
    expect(addVoucherItem).toHaveBeenCalledTimes(2);
    expect(host.additions).toBe(1);
  });

  it('should read the sectors once, the first time the voucher does not apply to everything', async () => {
    const { fixture, form, fill, save, addVoucherItem, sectorCombobox } = await setup();
    const choose = async (scope: DiscountScope) => {
      form.discountScope().value.set(scope);
      await fixture.whenStable();
    };

    expect(sectorCombobox).not.toHaveBeenCalled();
    await choose(DiscountScope.Custom);
    form.serviceCategories().value.set([DiscountServiceCategory.Bar]);
    await choose(DiscountScope.All);
    await choose(DiscountScope.WithoutCustom);
    expect(sectorCombobox).toHaveBeenCalledOnce();

    fill();
    await save();
    expect(addVoucherItem).toHaveBeenCalledWith(
      expect.objectContaining({
        discountScope: DiscountScope.WithoutCustom,
        serviceCategories: [DiscountServiceCategory.Bar],
      }),
    );
  });
});
