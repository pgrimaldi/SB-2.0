import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { DiscountScope } from '../../../entities/enums/discount-scope';
import { DiscountType } from '../../../entities/enums/discount-type';
import { VoucherAddItemRequest } from '../../../entities/settings/vouchers/voucher-add-item-request';
import { VoucherUpdateItemRequest } from '../../../entities/settings/vouchers/voucher-update-item-request';
import { VoucherService } from './voucher.service';

describe('VoucherService', () => {
  it('should send only the fields of the voucher, its periods and time slots, with the property of the session and at least 1 use', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const vouchers = TestBed.inject(VoucherService);
    const controller = TestBed.inject(HttpTestingController);
    const request = Object.assign(new VoucherAddItemRequest(), {
      voucherName: 'Estate',
      discountValue: 15,
      maxUseQuantity: null,
      idProperty: 'p2',
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

    vouchers.addVoucherItem('p1', request).subscribe();

    const body = controller.expectOne(`${environment.apiBaseUrl}/vouchers/add-voucher-item`).request
      .body;
    expect(body).toEqual({
      voucherName: 'Estate',
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
      discountScope: DiscountScope.All,
      maxUseQuantity: 1,
      isEnabled: true,
      idProperty: 'p1',
    });
    controller.verify();
  });

  it('should send the edited voucher with its id, like a new one, to update-voucher-item', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const vouchers = TestBed.inject(VoucherService);
    const controller = TestBed.inject(HttpTestingController);
    const request = Object.assign(new VoucherUpdateItemRequest(), {
      idVoucher: 'v1',
      voucherName: 'Estate',
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

    vouchers.updateVoucherItem('p1', request).subscribe();

    const body = controller.expectOne(`${environment.apiBaseUrl}/vouchers/update-voucher-item`)
      .request.body;
    expect(body).toEqual({
      idVoucher: 'v1',
      voucherName: 'Estate',
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
      maxUseQuantity: 1,
      isEnabled: true,
      idProperty: 'p1',
    });
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
        url === `${environment.apiBaseUrl}/vouchers/get-selected-item` &&
        params.get('idVoucher') === 'v1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });
});
