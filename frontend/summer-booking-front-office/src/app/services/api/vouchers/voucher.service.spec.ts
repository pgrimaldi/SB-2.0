import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { DiscountScope } from '../../../entities/enums/discount-scope';
import { DiscountServiceCategory } from '../../../entities/enums/discount-service-category';
import { DiscountType } from '../../../entities/enums/discount-type';
import { VoucherAddItemRequest } from '../../../entities/settings/vouchers/voucher-add-item-request';
import { VoucherUpdateItemRequest } from '../../../entities/settings/vouchers/voucher-update-item-request';
import { VoucherService } from './voucher.service';

describe('VoucherService', () => {
  it('should send only the fields of the voucher, its periods and time slots, without the property', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const vouchers = TestBed.inject(VoucherService);
    const controller = TestBed.inject(HttpTestingController);
    const request = Object.assign(new VoucherAddItemRequest(), {
      voucherName: 'Estate',
      voucherCode: 'ESTATE15',
      discountValue: 15,
      discountScope: DiscountScope.Custom,
      serviceCategories: [DiscountServiceCategory.Bar],
      idSectors: ['s1'],
      maxUseQuantity: null,
      validityPeriods: [
        {
          validFrom: '2026-08-01T00:00:00.000Z',
          validTo: '2026-08-31T00:00:00.000Z',
          daysOfWeek: [1, 2, 3],
          timeSlots: [{ from: '09:00', to: '12:00', note: 'extra-field' }],
          unexpectedFlag: 'extra-field',
        },
      ],
    });

    vouchers.addVoucherItem(request).subscribe();

    const body = controller.expectOne(
      `${environment.apiBaseUrl}/catalog/discounts/add-voucher-item`,
    ).request.body;
    expect(body).toEqual({
      voucherName: 'Estate',
      voucherCode: 'ESTATE15',
      isCodeRequired: true,
      validityPeriods: [
        {
          validFrom: '2026-08-01T00:00:00.000Z',
          validTo: '2026-08-31T00:00:00.000Z',
          daysOfWeek: [1, 2, 3],
          timeSlots: [{ from: '09:00', to: '12:00' }],
        },
      ],
      discountType: DiscountType.Percentage,
      discountValue: 15,
      discountScope: DiscountScope.Custom,
      serviceCategories: [DiscountServiceCategory.Bar],
      idSectors: ['s1'],
      maxUseQuantity: null,
      isEnabled: true,
    });
    controller.verify();
  });

  it('should send the edited voucher with its id, like a new one, and the version read to update-voucher-item, and answer the new version', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const vouchers = TestBed.inject(VoucherService);
    const controller = TestBed.inject(HttpTestingController);
    const request = Object.assign(new VoucherUpdateItemRequest(), {
      idVoucher: 'v1',
      voucherName: 'Estate',
      voucherCode: 'ESTATE15',
      isCodeRequired: false,
      discountType: DiscountType.Fixed,
      discountValue: 7.5,
      maxUseQuantity: null,
      validityPeriods: [
        {
          validFrom: '2026-08-01T00:00:00.000Z',
          validTo: '2026-08-31T00:00:00.000Z',
          daysOfWeek: [6, 7],
          timeSlots: [{ from: '10:00', to: '18:00', note: 'extra-field' }],
        },
      ],
    });

    let version: string | null | undefined;

    vouchers.updateVoucherItem(request, 'version-1').subscribe((answer) => (version = answer));

    const call = controller.expectOne(
      `${environment.apiBaseUrl}/catalog/discounts/update-voucher-item`,
    );
    expect(call.request.body).toEqual({
      idVoucher: 'v1',
      voucherName: 'Estate',
      voucherCode: 'ESTATE15',
      isCodeRequired: false,
      validityPeriods: [
        {
          validFrom: '2026-08-01T00:00:00.000Z',
          validTo: '2026-08-31T00:00:00.000Z',
          daysOfWeek: [6, 7],
          timeSlots: [{ from: '10:00', to: '18:00' }],
        },
      ],
      discountType: DiscountType.Fixed,
      discountValue: 7.5,
      discountScope: DiscountScope.All,
      serviceCategories: [],
      idSectors: [],
      maxUseQuantity: null,
      isEnabled: true,
    });
    expect(call.request.headers.get('If-Match')).toBe('"version-1"');
    call.flush(null, { status: 204, statusText: 'No Content', headers: { ETag: '"version-2"' } });
    expect(version).toBe('version-2');
    controller.verify();
  });

  it('should ask for one voucher by its id in the query string', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const vouchers = TestBed.inject(VoucherService);
    const controller = TestBed.inject(HttpTestingController);

    vouchers.selectedItem('v1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/discounts/get-selected-item` &&
        params.get('idVoucher') === 'v1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });
});
