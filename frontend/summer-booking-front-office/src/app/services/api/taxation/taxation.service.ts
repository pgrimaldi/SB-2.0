import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ElectronicReceiptResponse } from '../../../entities/settings/taxation/electronic-receipt-response';
import { PrinterItemListResponse } from '../../../entities/settings/taxation/printer-item-list-response';

/** GET: the property travels in the query string (`?idProperty=…`). */
@Injectable({ providedIn: 'root' })
export class TaxationService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/taxation`;

  readonly printerList = (idProperty: string): Observable<PrinterItemListResponse[]> =>
    this.httpClient.get<PrinterItemListResponse[]>(`${this.endpoint}/printer-list`, {
      params: { idProperty },
    });

  readonly electronicReceipt = (idProperty: string): Observable<ElectronicReceiptResponse> =>
    this.httpClient.get<ElectronicReceiptResponse>(`${this.endpoint}/electronic-receipt`, {
      params: { idProperty },
    });
}
