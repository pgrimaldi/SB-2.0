import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { trimmedText } from '../../../components/shared/forms/text-comparison';
import { isEmailAddress } from '../../../components/shared/forms/validators';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import {
  ContactSupportRequest,
  MAX_CONTACT_EMAIL_LENGTH,
  MAX_CONTACT_MESSAGE_LENGTH,
  MAX_CONTACT_NAME_LENGTH,
  MAX_CONTACT_PHONE_LENGTH,
} from '../../../entities/settings/contact-support/contact-support-request';
import { SupportInfo } from '../../../entities/settings/contact-support/support-info';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';

const MAX_LENGTHS: Readonly<Record<keyof ContactSupportRequest, number>> = {
  firstName: MAX_CONTACT_NAME_LENGTH,
  lastName: MAX_CONTACT_NAME_LENGTH,
  email: MAX_CONTACT_EMAIL_LENGTH,
  mobilePhone: MAX_CONTACT_PHONE_LENGTH,
  message: MAX_CONTACT_MESSAGE_LENGTH,
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
 * `GET /api/notification/support/info`: the hours in the language of `Accept-Language` (the mock has
 * only Italian and English).
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
 * `POST /api/notification/support/contact`: the mock only checks the message. One that starts with
 * "attesa" finds too many sent, one with "spento" a service that does not answer.
 */
export const contactSupportMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<ContactSupportRequest>;
  const errors = (Object.keys(MAX_LENGTHS) as (keyof ContactSupportRequest)[]).flatMap((field) =>
    fieldErrors(field, trimmedText(body[field] ?? null)),
  );
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  if (body.message?.startsWith('attesa')) {
    return problem(request, 429, ApiErrorCode.AuthTooManyAttempts);
  }
  if (body.message?.startsWith('spento')) {
    return problem(request, 503, ApiErrorCode.NotificationUnavailable);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(800));
};

function fieldErrors(field: keyof ContactSupportRequest, text: string | null): MockFieldError[] {
  const max = MAX_LENGTHS[field];
  if (!text) {
    return field === 'mobilePhone' ? [] : [{ field, code: ApiErrorCode.ValidationRequired }];
  }
  if (text.length > max) {
    return [{ field, code: ApiErrorCode.ValidationMaxLength, args: { max, min: 0 } }];
  }
  if (field === 'email' && !isEmailAddress(text)) {
    return [{ field, code: ApiErrorCode.ValidationEmail }];
  }
  return field === 'mobilePhone' && !/^[0-9 ]*$/.test(text)
    ? [{ field, code: ApiErrorCode.ValidationPhone }]
    : [];
}
