import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { VoucherItem } from '../../../entities/settings/vouchers/voucher-item';

@Injectable({ providedIn: 'root' })
export class VoucherService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/vouchers`;

  readonly voucherList = (request: PageRequest): Observable<Page<VoucherItem>> =>
    this.httpClient.post<Page<VoucherItem>>(`${this.endpoint}/voucher-list`, request);
}
