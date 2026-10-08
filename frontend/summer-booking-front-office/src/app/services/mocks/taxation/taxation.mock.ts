import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ElectronicReceiptResponse } from '../../../entities/settings/taxation/electronic-receipt-response';
import { PrinterItemListResponse } from '../../../entities/settings/taxation/printer-item-list-response';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { operationNotAllowed } from '../errors/problem.mock';
import { propertyOf } from '../properties/properties.mock';

/** Invented printers, with documentation IP addresses (RFC 5737). */
const PRINTERS: PrinterItemListResponse[] = [
  {
    idPrinter: '5f0c2a1e-8b3d-4c6f-9a2e-1d7b3c4e5f60',
    printerName: 'EPSON - 1',
    printerIP: '192.0.2.10',
    isEnabled: false,
  },
  {
    idPrinter: '9a4e6b2c-1d3f-4e5a-8b7c-2f6d1e3a4b50',
    printerName: 'EPSON - 2',
    printerIP: '192.0.2.11',
    isEnabled: true,
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
 * `GET /api/taxation/printer-list?idProperty=…`: the printers of the property. 401 without a valid
 * access token; 403 `operation.not_allowed` for a missing or unknown property.
 */
export const printerListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  if (!propertyOf(request.params.get('idProperty'))) {
    return operationNotAllowed(request);
  }
  return of(new HttpResponse({ status: 200, url: request.url, body: PRINTERS })).pipe(delay(150));
};

/**
 * `GET /api/taxation/electronic-receipt?idProperty=…`: the terms of the electronic receipt as HTML,
 * in the language of `Accept-Language` (Italian when it is not English). 401 without a valid access
 * token; 403 `operation.not_allowed` for a missing or unknown property.
 */
export const electronicReceiptMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  if (!propertyOf(request.params.get('idProperty'))) {
    return operationNotAllowed(request);
  }
  const language = request.headers.get('Accept-Language')?.startsWith('en') ? 'en' : 'it';
  const body: ElectronicReceiptResponse = { termsAndConditions: TERMS[language] };
  return of(new HttpResponse({ status: 200, url: request.url, body })).pipe(delay(150));
};
