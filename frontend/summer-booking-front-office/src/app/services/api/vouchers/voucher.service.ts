import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { VoucherAddItemRequest } from '../../../entities/settings/vouchers/voucher-add-item-request';
import { VoucherItem } from '../../../entities/settings/vouchers/voucher-item';
import { VoucherSelectedItem } from '../../../entities/settings/vouchers/voucher-selected-item';
import { VoucherTimeSlot } from '../../../entities/settings/vouchers/voucher-time-slot';
import { VoucherUpdateItemRequest } from '../../../entities/settings/vouchers/voucher-update-item-request';
import { VoucherValidityPeriod } from '../../../entities/settings/vouchers/voucher-validity-period';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class VoucherService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/vouchers`;

  readonly voucherList = (request: PageRequest): Observable<Page<VoucherItem>> =>
    this.httpClient.post<Page<VoucherItem>>(`${this.endpoint}/voucher-list`, request);

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deleteVoucherItems = (idVouchers: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-voucher-item`, { idVouchers });

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicateVoucherItems = (idVouchers: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/duplicate-voucher-item`, { idVouchers });

  /** Answers 204 with no body. */
  readonly addVoucherItem = (request: VoucherAddItemRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/add-voucher-item`, {
      ...onlyFieldsOf(VoucherAddItemRequest, request),
      maxUseQuantity: request.maxUseQuantity ?? 1,
      validityPeriods: request.validityPeriods.map((period) => ({
        ...onlyFieldsOf(VoucherValidityPeriod, period),
        timeSlots: period.timeSlots.map((timeSlot) => onlyFieldsOf(VoucherTimeSlot, timeSlot)),
      })),
    });

  /** Answers 204 with no body. */
  readonly updateVoucherItem = (request: VoucherUpdateItemRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-voucher-item`, {
      ...onlyFieldsOf(VoucherUpdateItemRequest, request),
      maxUseQuantity: request.maxUseQuantity ?? 1,
      validityPeriods: request.validityPeriods.map((period) => ({
        ...onlyFieldsOf(VoucherValidityPeriod, period),
        timeSlots: period.timeSlots.map((timeSlot) => onlyFieldsOf(VoucherTimeSlot, timeSlot)),
      })),
    });

  readonly selectedItem = (idVoucher: string): Observable<VoucherSelectedItem> =>
    this.httpClient.get<VoucherSelectedItem>(`${this.endpoint}/get-selected-item`, {
      params: { idVoucher },
    });
}
