import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ContactSupportRequest } from '../../../entities/system/contact-support-request';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';
import { MOCK_PROPERTIES } from '../properties/properties.mock';

const REQUIRED_FIELDS = ['firstName', 'lastName', 'email', 'message'] as const;

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
  if (!MOCK_PROPERTIES.some((row) => row.publicId === body.idProperty)) {
    errors.push({ field: 'idProperty', code: 'validation.invalid_value' });
  }
  for (const field of REQUIRED_FIELDS) {
    const value = body[field];
    if (typeof value !== 'string' || !value.trim()) {
      errors.push({ field, code: 'validation.invalid_value' });
    }
  }
  if (body.mobilePhone !== null && typeof body.mobilePhone !== 'string') {
    errors.push({ field: 'mobilePhone', code: 'validation.invalid_value' });
  }
  if (errors.length) {
    return problem(request, 400, 'validation.invalid_request', {
      title: 'Invalid request',
      errors,
    });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(800));
};
