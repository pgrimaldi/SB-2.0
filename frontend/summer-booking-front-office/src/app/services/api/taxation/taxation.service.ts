import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { AddPrinterItemRequest } from '../../../entities/settings/taxation/add-printer-item-request';
import { ElectronicReceiptAccount } from '../../../entities/settings/taxation/electronic-receipt-account';
import { PrinterItemListResponse } from '../../../entities/settings/taxation/printer-item-list-response';
import { SetPrinterIsActiveRequest } from '../../../entities/settings/taxation/set-printer-is-active-request';
import { readBlobError } from '../errors/read-blob-error';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class TaxationService {
  private readonly httpClient = inject(HttpClient);
  private readonly printersEndpoint = `${environment.apiBaseUrl}/fiscalprint/printers`;
  private readonly receiptEndpoint = `${environment.apiBaseUrl}/fiskaly`;

  readonly printerList = (request: PageRequest): Observable<Page<PrinterItemListResponse>> =>
    this.httpClient.post<Page<PrinterItemListResponse>>(
      `${this.printersEndpoint}/printer-list`,
      request,
    );

  /** The terms and conditions of the electronic receipt as HTML, the body is the text alone. */
  readonly electronicReceipt = (): Observable<string | null> =>
    this.httpClient.get<string | null>(`${this.receiptEndpoint}/electronic-receipt`);

  /** Answers 204 with no body. */
  readonly setPrinterIsActive = (request: SetPrinterIsActiveRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.printersEndpoint}/set-is-active`, request);

  /** Answers 204 with no body. */
  readonly addPrinter = (printer: AddPrinterItemRequest): Observable<void> =>
    this.httpClient.post<void>(
      `${this.printersEndpoint}/add-printer`,
      onlyFieldsOf(AddPrinterItemRequest, printer),
    );

  /** Answers 204 with no body. */
  readonly activateElectronicReceipt = (isElectronicReceiptAccepted: boolean): Observable<void> =>
    this.httpClient.post<void>(`${this.receiptEndpoint}/electronic-receipt-activation`, {
      isElectronicReceiptAccepted,
    });

  /** Answers 204 with no body. */
  readonly saveElectronicReceiptAccount = (account: ElectronicReceiptAccount): Observable<void> =>
    this.httpClient.post<void>(
      `${this.receiptEndpoint}/save-electronic-receipt-account`,
      onlyFieldsOf(ElectronicReceiptAccount, account),
    );

  /** Answers 204 with no body. */
  readonly deletePrinter = (idPrinter: string): Observable<void> =>
    this.httpClient.post<void>(`${this.printersEndpoint}/delete-printer`, { idPrinter });

  /**
   * The guide as a file, of a type not decided yet: the whole answer, whose headers give the file
   * name (`Content-Disposition`).
   */
  readonly downloadGuideElectronicReceipt = (): Observable<HttpResponse<Blob>> =>
    this.httpClient
      .post(`${this.receiptEndpoint}/download-guide-electronic-receipt`, null, {
        observe: 'response',
        responseType: 'blob',
      })
      .pipe(catchError(readBlobError));
}
