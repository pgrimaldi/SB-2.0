import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { AddPrinterItemRequest } from '../../../entities/settings/taxation/add-printer-item-request';
import { DeletePrinterRequest } from '../../../entities/settings/taxation/delete-printer-request';
import { ElectronicReceiptAccount } from '../../../entities/settings/taxation/electronic-receipt-account';
import { ElectronicReceiptActivationRequest } from '../../../entities/settings/taxation/electronic-receipt-activation-request';
import { ElectronicReceiptResponse } from '../../../entities/settings/taxation/electronic-receipt-response';
import { PrinterItemListResponse } from '../../../entities/settings/taxation/printer-item-list-response';
import { SetPrinterIsActiveRequest } from '../../../entities/settings/taxation/set-printer-is-active-request';
import { readBlobError } from '../errors/read-blob-error';
import { onlyFieldsOf } from '../only-fields-of';

/** With GET the property travels in the query string (`?idProperty=…`), with POST in the body. */
@Injectable({ providedIn: 'root' })
export class TaxationService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/taxation`;

  readonly printerList = (request: PageRequest): Observable<Page<PrinterItemListResponse>> =>
    this.httpClient.post<Page<PrinterItemListResponse>>(`${this.endpoint}/printer-list`, request);

  readonly electronicReceipt = (idProperty: string): Observable<ElectronicReceiptResponse> =>
    this.httpClient.get<ElectronicReceiptResponse>(`${this.endpoint}/electronic-receipt`, {
      params: { idProperty },
    });

  /** Answers 204 with no body. */
  readonly setPrinterIsActive = (request: SetPrinterIsActiveRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/set-is-active`, request);

  /** Answers 204 with no body. */
  readonly addPrinter = (idProperty: string, printer: AddPrinterItemRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/add-printer`, {
      ...onlyFieldsOf(AddPrinterItemRequest, printer),
      idProperty,
    });

  /** Answers 204 with no body. */
  readonly activateElectronicReceipt = (
    request: ElectronicReceiptActivationRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/electronic-receipt-activation`, request);

  /** Answers 204 with no body. */
  readonly saveElectronicReceiptAccount = (
    idProperty: string,
    account: ElectronicReceiptAccount,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/save-electronic-receipt-account`, {
      ...onlyFieldsOf(ElectronicReceiptAccount, account),
      idProperty,
    });

  /** Answers 204 with no body. */
  readonly deletePrinter = (request: DeletePrinterRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-printer`, request);

  /**
   * The guide as a file, of a type not decided yet: the whole answer, whose headers give the file
   * name (`Content-Disposition`).
   */
  readonly downloadGuideElectronicReceipt = (idProperty: string): Observable<HttpResponse<Blob>> =>
    this.httpClient
      .post(
        `${this.endpoint}/download-guide-electronic-receipt`,
        { idProperty },
        {
          observe: 'response',
          responseType: 'blob',
        },
      )
      .pipe(catchError(readBlobError));
}
