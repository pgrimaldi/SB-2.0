import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { DeleteVoucherItemsRequest } from '../../../entities/settings/vouchers/delete-voucher-items-request';
import { DuplicateVoucherItemsRequest } from '../../../entities/settings/vouchers/duplicate-voucher-items-request';
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
  readonly deleteVoucherItems = (request: DeleteVoucherItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-voucher-item`, request);

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicateVoucherItems = (request: DuplicateVoucherItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/duplicate-voucher-item`, request);

  /** Answers 204 with no body. */
  readonly addVoucherItem = (
    idProperty: string,
    request: VoucherAddItemRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/add-voucher-item`, {
      ...onlyFieldsOf(VoucherAddItemRequest, request),
      maxUseQuantity: request.maxUseQuantity ?? 1,
      validityPeriods: request.validityPeriods.map((period) => ({
        ...onlyFieldsOf(VoucherValidityPeriod, period),
        timeSlots: period.timeSlots.map((timeSlot) => onlyFieldsOf(VoucherTimeSlot, timeSlot)),
      })),
      idProperty,
    });

  /** Answers 204 with no body. */
  readonly updateVoucherItem = (
    idProperty: string,
    request: VoucherUpdateItemRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-voucher-item`, {
      ...onlyFieldsOf(VoucherUpdateItemRequest, request),
      maxUseQuantity: request.maxUseQuantity ?? 1,
      validityPeriods: request.validityPeriods.map((period) => ({
        ...onlyFieldsOf(VoucherValidityPeriod, period),
        timeSlots: period.timeSlots.map((timeSlot) => onlyFieldsOf(VoucherTimeSlot, timeSlot)),
      })),
      idProperty,
    });

  readonly selectedItem = (idVoucher: string): Observable<VoucherSelectedItem> =>
    this.httpClient.get<VoucherSelectedItem>(`${this.endpoint}/get-selected-item`, {
      params: { idVoucher },
    });
}
