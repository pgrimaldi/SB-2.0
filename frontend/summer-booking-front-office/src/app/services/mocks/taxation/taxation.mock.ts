import { HttpEvent, HttpHeaders, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { PrinterMode } from '../../../entities/enums/printer-mode';
import { PrinterModel } from '../../../entities/enums/printer-model';
import { trimmedText } from '../../../components/shared/forms/text-comparison';
import {
  AddPrinterItemRequest,
  MAX_PRINTER_IP_LENGTH,
  MAX_PRINTER_LINE_LENGTH,
  MAX_PRINTER_NAME_LENGTH,
  MAX_PRINTER_PORT,
  PRINTER_DEPARTMENTS_COUNT,
} from '../../../entities/settings/taxation/add-printer-item-request';
import {
  ElectronicReceiptAccount,
  MAX_ACCOUNT_NAME_LENGTH,
  MAX_ACCOUNT_POSTAL_CODE_LENGTH,
  MAX_ACCOUNT_STREET_NUMBER_LENGTH,
  MAX_ACCOUNT_TAX_CODE_LENGTH,
  MAX_ACCOUNT_VAT_NUMBER_LENGTH,
} from '../../../entities/settings/taxation/electronic-receipt-account';
import { PrinterItemListResponse } from '../../../entities/settings/taxation/printer-item-list-response';
import { SetPrinterIsActiveRequest } from '../../../entities/settings/taxation/set-printer-is-active-request';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';

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

/** `POST /api/fiscalprint/printers/printer-list`: one page of the printers, by name with the numbers as numbers. */
export const printerListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const page = positiveInteger(body.page, 1);
  const pageSize = positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE);
  const sorted = [...printers].sort((first, second) =>
    (first.printerName ?? '').localeCompare(second.printerName ?? '', 'it', {
      sensitivity: 'base',
      numeric: true,
    }),
  );
  const answer: Page<PrinterItemListResponse> = {
    total: sorted.length,
    rows: sorted.slice((page - 1) * pageSize, page * pageSize).map((printer) => ({ ...printer })),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}

/** `POST /api/fiscalprint/printers/set-is-active`: switched on, the other one of its mode goes off. */
export const setPrinterIsActiveMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<SetPrinterIsActiveRequest>;
  if (typeof body.isEnabled !== 'boolean') {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors: [{ field: 'isEnabled', code: ApiErrorCode.ValidationRequired }],
    });
  }
  const printer = printers.find(({ idPrinter }) => idPrinter === body.idPrinter);
  if (!printer) {
    return problem(request, 404, ApiErrorCode.FiscalPrintPrinterNotFound);
  }
  if (body.isEnabled) {
    printers
      .filter(({ printerMode }) => printerMode === printer.printerMode)
      .forEach((other) => (other.isEnabled = false));
  }
  printer.isEnabled = body.isEnabled;
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** `POST /api/fiscalprint/printers/delete-printer`. */
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

/** `POST /api/fiscalprint/printers/add-printer`: the new printer starts off. */
export const addPrinterMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<AddPrinterItemRequest>;
  const errors = printerErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  printers = [
    ...printers,
    {
      idPrinter: crypto.randomUUID(),
      printerName: trimmedText(body.printerName ?? null),
      printerIP: trimmedText(body.printerIP ?? null),
      isEnabled: false,
      printerMode: body.printerMode ?? null,
    },
  ];
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** One error per wrong field, as the backend answers them. */
function printerErrors(body: Partial<AddPrinterItemRequest>): MockFieldError[] {
  const errors: MockFieldError[] = [
    ...textErrors('printerName', body.printerName, MAX_PRINTER_NAME_LENGTH, true),
    ...textErrors('printerIP', body.printerIP, MAX_PRINTER_IP_LENGTH, true),
    ...textErrors('firstHeader', body.firstHeader, MAX_PRINTER_LINE_LENGTH),
    ...textErrors('secondHeader', body.secondHeader, MAX_PRINTER_LINE_LENGTH),
    ...textErrors('footer', body.footer, MAX_PRINTER_LINE_LENGTH),
    ...choiceErrors('printerModel', body.printerModel, Object.values(PrinterModel)),
    ...choiceErrors('printerMode', body.printerMode, Object.values(PrinterMode)),
  ];
  const port = body.printerPort ?? null;
  if (port === null) {
    errors.push({ field: 'printerPort', code: ApiErrorCode.ValidationRequired });
  } else if (!Number.isInteger(port) || port < 1 || port > MAX_PRINTER_PORT) {
    errors.push({
      field: 'printerPort',
      code: ApiErrorCode.ValidationOutOfRange,
      args: { from: 1, to: MAX_PRINTER_PORT },
    });
  }
  const rates = body.departmentVatRates;
  const chosen = (rates ?? []).filter((rate) => rate !== null);
  if (!rates) {
    errors.push({ field: 'departmentVatRates', code: ApiErrorCode.ValidationRequired });
  } else if (
    rates.length !== PRINTER_DEPARTMENTS_COUNT ||
    new Set(chosen).size !== chosen.length ||
    chosen.some((rate) => rate < 0 || rate > 100 || Math.round(rate * 100) / 100 !== rate) ||
    (chosen.length > 0 && !!body.printerMode && body.printerMode !== PrinterMode.Fiscal)
  ) {
    errors.push({ field: 'departmentVatRates', code: ApiErrorCode.ValidationInvalid });
  }
  return errors;
}

function textErrors(
  field: string,
  value: string | null | undefined,
  max?: number,
  isRequired = false,
): MockFieldError[] {
  const text = trimmedText(value ?? null);
  if (!text) {
    return isRequired ? [{ field, code: ApiErrorCode.ValidationRequired }] : [];
  }
  return max !== undefined && text.length > max
    ? [{ field, code: ApiErrorCode.ValidationMaxLength, args: { max, min: 0 } }]
    : [];
}

function choiceErrors(
  field: string,
  value: string | null | undefined,
  choices: readonly string[],
): MockFieldError[] {
  if (!value) {
    return [{ field, code: ApiErrorCode.ValidationRequired }];
  }
  return choices.includes(value) ? [] : [{ field, code: ApiErrorCode.ValidationInvalid }];
}

/** What the account of the property has: the mock keeps it until the page is loaded again. */
const account = { areTermsAccepted: false, isPinSaved: false, isPasswordSaved: false };

/** `GET /api/fiskaly/electronic-receipt`: the terms as HTML, in Italian when the language is not English. */
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

/** `POST /api/fiskaly/electronic-receipt-activation`. */
export const electronicReceiptActivationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { isElectronicReceiptAccepted?: boolean };
  if (body.isElectronicReceiptAccepted !== true) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors: [{ field: 'isElectronicReceiptAccepted', code: ApiErrorCode.ValidationMustAccept }],
    });
  }
  account.areTermsAccepted = true;
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** `POST /api/fiskaly/save-electronic-receipt-account`: PIN and password null keep the saved ones. */
export const saveElectronicReceiptAccountMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<ElectronicReceiptAccount>;
  const errors: MockFieldError[] = [
    ...textErrors('companyName', body.companyName, MAX_ACCOUNT_NAME_LENGTH, true),
    ...textErrors('companyAddress', body.companyAddress, MAX_ACCOUNT_NAME_LENGTH, true),
    ...textErrors('streetNumber', body.streetNumber, MAX_ACCOUNT_STREET_NUMBER_LENGTH, true),
    ...textErrors('postalCode', body.postalCode, MAX_ACCOUNT_POSTAL_CODE_LENGTH, true),
    ...textErrors('city', body.city, MAX_ACCOUNT_NAME_LENGTH, true),
    ...textErrors('country', body.country, undefined, true),
    ...textErrors('taxCode', body.taxCode, MAX_ACCOUNT_TAX_CODE_LENGTH, true),
    ...textErrors('revenueAgencyCode', body.revenueAgencyCode, undefined, true),
    ...textErrors('vatNumber', body.vatNumber, MAX_ACCOUNT_VAT_NUMBER_LENGTH, true),
  ];
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  if (!account.areTermsAccepted) {
    return problem(request, 409, ApiErrorCode.FiskalyTermsNotAccepted);
  }
  const secretErrors = [
    ...textErrors('pin', body.pin, undefined, !account.isPinSaved),
    ...textErrors('password', body.password, undefined, !account.isPasswordSaved),
  ];
  if (secretErrors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors: secretErrors });
  }
  account.isPinSaved = true;
  account.isPasswordSaved = true;
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** `POST /api/fiskaly/download-guide-electronic-receipt`: an invented text file, the real one is not known yet. */
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
