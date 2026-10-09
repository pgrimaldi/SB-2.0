import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { DeletePrinterRequest } from '../../../entities/settings/taxation/delete-printer-request';
import { DownloadGuideElectronicReceiptRequest } from '../../../entities/settings/taxation/download-guide-electronic-receipt-request';
import { ElectronicReceiptResponse } from '../../../entities/settings/taxation/electronic-receipt-response';
import { PrinterItemListResponse } from '../../../entities/settings/taxation/printer-item-list-response';
import { SetPrinterIsActiveRequest } from '../../../entities/settings/taxation/set-printer-is-active-request';
import { readBlobError } from '../errors/read-blob-error';

/** With GET the property travels in the query string (`?idProperty=…`), with POST in the body. */
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

  /** Answers 204 with no body. */
  readonly setPrinterIsActive = (request: SetPrinterIsActiveRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/set-is-active`, request);

  /** Answers 204 with no body. */
  readonly deletePrinter = (request: DeletePrinterRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-printer`, request);

  /**
   * The guide as a file, of a type not decided yet: the whole answer, whose headers give the file
   * name (`Content-Disposition`).
   */
  readonly downloadGuideElectronicReceipt = (
    request: DownloadGuideElectronicReceiptRequest,
  ): Observable<HttpResponse<Blob>> =>
    this.httpClient
      .post(`${this.endpoint}/download-guide-electronic-receipt`, request, {
        observe: 'response',
        responseType: 'blob',
      })
      .pipe(catchError(readBlobError));
}
