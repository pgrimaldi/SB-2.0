import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { ContactSupportRequest } from '../../../entities/settings/contact-support/contact-support-request';
import {
  EmailConfigurationData,
  SmtpSecurity,
} from '../../../entities/settings/email-configuration/email-configuration-data';
import {
  EmailConfigurationRequest,
  SMTP_CONNECTION_FIELDS,
} from '../../../entities/settings/email-configuration/email-configuration-request';
import { SupportInfo } from '../../../entities/settings/contact-support/support-info';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';

const REQUIRED_FIELDS = ['firstName', 'lastName', 'email', 'message'] as const;
const SMTP_SECURITIES: readonly SmtpSecurity[] = ['None', 'Ssl', 'Tls'];
const REQUIRED_EMAIL_FIELDS = ['senderMailAddress', 'smtpServerAddress', 'smtpUsername'] as const;

/** Invented values: never a real mail server or real credentials. */
const EMAIL_CONFIGURATION: Omit<EmailConfigurationData, 'hasSmtpPassword'> = {
  senderMailAddress: 'prenotazioni@lido-demo.example',
  senderName: 'Lido Demo',
  smtpServerAddress: 'smtp.lido-demo.example',
  smtpPort: 587,
  smtpUsername: 'prenotazioni@lido-demo.example',
  smtpSecurity: 'Tls',
};
/** Kept apart, like the real server keeps it encrypted: it is never part of an answer. */
let smtpPassword: string | null = 'password-di-esempio';

/**
 * `GET /api/system/email-configuration`: the mail settings of the property. 401 without a valid
 * access token.
 */
export const emailConfigurationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  return of(
    new HttpResponse({
      status: 200,
      url: request.url,
      body: { ...EMAIL_CONFIGURATION, hasSmtpPassword: smtpPassword !== null },
    }),
  ).pipe(delay(150));
};

/**
 * `GET /api/system/get-stripe-information`: the id of the Stripe account connected to the property,
 * the body is the string alone. 401 without a valid access token.
 */
export const stripeInformationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  return of(
    new HttpResponse({ status: 200, url: request.url, body: 'acct_1QmDemo4Fz8Lw2Xc' }),
  ).pipe(delay(150));
};

const SUPPORT_HOURS: Record<'it' | 'en', string[]> = {
  it: [
    '1 maggio - 30 settembre, dal lunedì alla domenica: dalle 8:30 alle 20:00',
    '1 ottobre - 30 aprile, dal lunedì al venerdì: dalle 9:00 alle 18:00',
  ],
  en: [
    'May 1 - September 30, Monday to Sunday: 8:30 am to 8:00 pm',
    'October 1 - April 30, Monday to Friday: 9:00 am to 6:00 pm',
  ],
};

/**
 * `POST /api/system/send-test-email` with `EmailConfigurationRequest`: the real backend tries the
 * settings by sending an email, the mock only checks them (see `emailConfigurationErrors`). 204
 * without a body; 401 without a valid access token; 400 with one error per wrong field.
 */
export const sendTestEmailMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const errors = emailConfigurationErrors(request.body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors,
    });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(1000));
};

/**
 * `POST /api/system/save-email-configuration` with `EmailConfigurationRequest`: same checks as the
 * test email; the mock keeps the values until the page is reloaded, so the GET (and Reset) answers
 * them. 204 without a body; 401 without a valid access token; 400 with one error per wrong field.
 */
export const saveEmailConfigurationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const errors = emailConfigurationErrors(request.body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors,
    });
  }
  const body = request.body as EmailConfigurationRequest;
  Object.assign(EMAIL_CONFIGURATION, {
    senderMailAddress: body.senderMailAddress as string,
    senderName: body.senderName as string,
    smtpServerAddress: body.smtpServerAddress as string,
    smtpPort: body.smtpPort as number,
    smtpUsername: body.smtpUsername as string,
    smtpSecurity: body.smtpSecurity as SmtpSecurity,
  });
  smtpPassword = body.smtpPassword ?? smtpPassword;
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * Sender, server and username filled, port from 1 to 65535, a known security; the password a new
 * non-empty text, or `null` (keep the saved one) only for the same server, port, user and security:
 * the saved password is never sent anywhere else.
 */
function emailConfigurationErrors(requestBody: unknown): MockFieldError[] {
  const body = (requestBody ?? {}) as Partial<Record<keyof EmailConfigurationRequest, unknown>>;
  const errors: MockFieldError[] = [];
  for (const field of REQUIRED_EMAIL_FIELDS) {
    const value = body[field];
    if (typeof value !== 'string' || !value.trim()) {
      errors.push({ field, code: ApiErrorCode.ValidationInvalid });
    }
  }
  const port = body.smtpPort;
  if (!Number.isInteger(port) || (port as number) < 1 || (port as number) > 65535) {
    errors.push({ field: 'smtpPort', code: ApiErrorCode.ValidationInvalid });
  }
  if (!SMTP_SECURITIES.includes(body.smtpSecurity as SmtpSecurity)) {
    errors.push({ field: 'smtpSecurity', code: ApiErrorCode.ValidationInvalid });
  }
  const otherConnection = SMTP_CONNECTION_FIELDS.some(
    (field) => body[field] !== EMAIL_CONFIGURATION[field],
  );
  if (
    body.smtpPassword === null
      ? smtpPassword !== null && otherConnection
      : typeof body.smtpPassword !== 'string' || !body.smtpPassword
  ) {
    errors.push({ field: 'smtpPassword', code: ApiErrorCode.ValidationInvalid });
  }
  return errors;
}

/**
 * `POST /api/system/info-support` without a body: the support contacts and hours, the hours in the
 * language of `Accept-Language` (Italian when it is not English). 401 without a valid access token.
 */
export const supportInfoMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const language = request.headers.get('Accept-Language')?.startsWith('en') ? 'en' : 'it';
  const info: SupportInfo = {
    phoneNumber: '050 7916620',
    mailAddress: 'info@summerbooking.it',
    supportHour: SUPPORT_HOURS[language],
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: info })).pipe(delay(150));
};

/**
 * `POST /api/system/contact-support` with `ContactSupportRequest`: the real backend sends the
 * message to the support team, the mock only checks it. 204 without a body; 401 without a valid
 * access token; 400 with one error per missing field (blank counts as missing; the mobile phone may
 * be `null`).
 */
export const contactSupportMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<Record<keyof ContactSupportRequest, unknown>>;
  const errors: MockFieldError[] = [];
  for (const field of REQUIRED_FIELDS) {
    const value = body[field];
    if (typeof value !== 'string' || !value.trim()) {
      errors.push({ field, code: ApiErrorCode.ValidationInvalid });
    }
  }
  if (body.mobilePhone !== null && typeof body.mobilePhone !== 'string') {
    errors.push({ field: 'mobilePhone', code: ApiErrorCode.ValidationInvalid });
  }
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors,
    });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(800));
};
