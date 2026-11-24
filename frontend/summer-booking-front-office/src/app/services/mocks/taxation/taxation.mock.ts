import { HttpEvent, HttpHeaders, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { PrinterMode } from '../../../entities/enums/printer-mode';
import { AddPrinterItemRequest } from '../../../entities/settings/taxation/add-printer-item-request';
import { PrinterItemListResponse } from '../../../entities/settings/taxation/printer-item-list-response';
import { SetPrinterIsActiveRequest } from '../../../entities/settings/taxation/set-printer-is-active-request';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { problem } from '../errors/problem.mock';

/** Invented printers, with documentation IP addresses (RFC 5737); changed by the mock calls. */
let printers: PrinterItemListResponse[] = [
  {
    idPrinter: '5f0c2a1e-8b3d-4c6f-9a2e-1d7b3c4e5f60',
    printerName: 'EPSON - 1',
    printerIP: '192.0.2.10',
    isEnabled: false,
    printerMode: PrinterMode.Fiscal,
  },
  {
    idPrinter: '9a4e6b2c-1d3f-4e5a-8b7c-2f6d1e3a4b50',
    printerName: 'EPSON - 2',
    printerIP: '192.0.2.11',
    isEnabled: true,
    printerMode: PrinterMode.Fiscal,
  },
  {
    idPrinter: '7c3d9e1f-2a4b-4c5d-8e6f-0a1b2c3d4e5f',
    printerName: 'Comande bar',
    printerIP: '192.0.2.12',
    isEnabled: true,
    printerMode: PrinterMode.Orders,
  },
];

/** Invented sample text: the real terms come from the backend. */
const TERMS: Record<'it' | 'en', string> = {
  it: [
    '<h4>Generale</h4>',
    '<p>Testo di prova dei termini e condizioni del servizio di scontrino elettronico.</p>',
    '<h4>Protezione dei dati</h4>',
    '<p>I dati sono trattati secondo l\'<a href="https://example.com/privacy">informativa privacy</a>.</p>',
    '<h4>Uso del servizio</h4>',
    '<ul><li>Usare il servizio solo per lo stabilimento.</li><li>Non condividere le credenziali.</li></ul>',
    '<h4>Contatto</h4>',
    '<p>Per domande: <a href="mailto:assistenza@example.com">assistenza@example.com</a></p>',
  ].join(''),
  en: [
    '<h4>General</h4>',
    '<p>Sample text of the terms and conditions of the electronic receipt service.</p>',
    '<h4>Data protection</h4>',
    '<p>Data is processed according to the <a href="https://example.com/privacy">privacy policy</a>.</p>',
    '<h4>Use of the service</h4>',
    '<ul><li>Use the service only for the property.</li><li>Do not share the credentials.</li></ul>',
    '<h4>Contact</h4>',
    '<p>For questions: <a href="mailto:assistenza@example.com">assistenza@example.com</a></p>',
  ].join(''),
};

/**
 * `POST /api/fiscalprint/printers/printer-list` with `PageRequest`: one page of the printers of the property;
 * search, filters and sorting are not read. 401 without a valid access token.
 */
export const printerListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const page = positiveInteger(body.page, 1);
  const pageSize = positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE);
  // Copies, as from a real server: the page never holds the mock's own objects.
  const answer: Page<PrinterItemListResponse> = {
    total: printers.length,
    rows: printers.slice((page - 1) * pageSize, page * pageSize).map((printer) => ({ ...printer })),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}

/**
 * `POST /api/fiscalprint/printers/set-is-active` with `SetPrinterIsActiveRequest`: switches the printer on or
 * off; switched on, the other one of its mode goes off. 204 without a body; 401 without a valid access token; 404 for an unknown printer.
 */
export const setPrinterIsActiveMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<SetPrinterIsActiveRequest>;
  const printer = printers.find(({ idPrinter }) => idPrinter === body.idPrinter);
  if (!printer) {
    return problem(request, 404, ApiErrorCode.FiscalPrintPrinterNotFound);
  }
  if (body.isEnabled === true) {
    printers
      .filter(({ printerMode }) => printerMode === printer.printerMode)
      .forEach((other) => (other.isEnabled = false));
  }
  printer.isEnabled = body.isEnabled === true;
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/fiscalprint/printers/delete-printer` with `{ idPrinter }`. 204 without a body; 401 without a valid
 * access token; 404 for an unknown printer.
 */
export const deletePrinterMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idPrinter?: string };
  if (!printers.some(({ idPrinter }) => idPrinter === body.idPrinter)) {
    return problem(request, 404, ApiErrorCode.FiscalPrintPrinterNotFound);
  }
  printers = printers.filter(({ idPrinter }) => idPrinter !== body.idPrinter);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `GET /api/fiskaly/electronic-receipt`: the terms of the electronic receipt as HTML, in the
 * language of `Accept-Language` (Italian when it is not English). 401 without a valid access token.
 */
export const electronicReceiptMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const language = request.headers.get('Accept-Language')?.startsWith('en') ? 'en' : 'it';
  return of(new HttpResponse({ status: 200, url: request.url, body: TERMS[language] })).pipe(
    delay(150),
  );
};

/**
 * `POST /api/fiskaly/download-guide-electronic-receipt` without a body: the guide as a file. The
 * real document and its type are not known yet: the mock sends an invented text file. 401 without a
 * valid access token.
 */
export const downloadGuideElectronicReceiptMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const guide = new Blob(['Guida di prova dello scontrino elettronico.'], {
    type: 'text/plain',
  });
  const headers = new HttpHeaders({
    'Content-Type': 'text/plain',
    'Content-Disposition': 'attachment; filename="guida-scontrino-elettronico.txt"',
  });
  return of(new HttpResponse({ status: 200, url: request.url, headers, body: guide })).pipe(
    delay(600),
  );
};

/**
 * `POST /api/fiscalprint/printers/add-printer` with `AddPrinterItemRequest`: the new printer is added switched
 * off. 204 without a body; 401 without a valid access token.
 */
export const addPrinterMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<AddPrinterItemRequest>;
  printers = [
    ...printers,
    {
      idPrinter: crypto.randomUUID(),
      printerName: body.printerName ?? null,
      printerIP: body.printerIP ?? null,
      isEnabled: false,
      printerMode: body.printerMode ?? null,
    },
  ];
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/fiskaly/electronic-receipt-activation` with `{ isElectronicReceiptAccepted }`. 204
 * without a body; 401 without a valid access token.
 */
export const electronicReceiptActivationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/fiskaly/save-electronic-receipt-account` with `ElectronicReceiptAccount`. 204 without
 * a body; 401 without a valid access token.
 */
export const saveElectronicReceiptAccountMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};
