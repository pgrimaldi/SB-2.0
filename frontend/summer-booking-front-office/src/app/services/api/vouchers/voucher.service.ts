import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { DeleteVoucherItemsRequest } from '../../../entities/settings/vouchers/delete-voucher-items-request';
import { DuplicateVoucherItemsRequest } from '../../../entities/settings/vouchers/duplicate-voucher-items-request';
import { VoucherItem } from '../../../entities/settings/vouchers/voucher-item';

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
}
