import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { ContactSupportRequest } from '../../../entities/settings/contact-support/contact-support-request';
import { SupportInfo } from '../../../entities/settings/contact-support/support-info';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';

const REQUIRED_FIELDS = ['firstName', 'lastName', 'email', 'message'] as const;

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
