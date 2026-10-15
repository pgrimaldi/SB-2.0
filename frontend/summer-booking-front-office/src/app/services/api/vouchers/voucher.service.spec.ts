import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { DiscountScope } from '../../../entities/enums/discount-scope';
import { DiscountType } from '../../../entities/enums/discount-type';
import { VoucherAddItemRequest } from '../../../entities/settings/vouchers/voucher-add-item-request';
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
});
