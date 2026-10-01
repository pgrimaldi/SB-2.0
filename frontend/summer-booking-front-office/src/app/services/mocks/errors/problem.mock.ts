import { HttpErrorResponse, HttpRequest } from '@angular/common/http';
import { Observable, delay, throwError } from 'rxjs';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { ApiFieldError, ApiProblemArgs } from '../../../entities/errors/api-problem';

const STATUS_TEXTS: Readonly<Record<number, string>> = {
  400: 'Bad Request',
  401: 'Unauthorized',
  403: 'Forbidden',
  404: 'Not Found',
  409: 'Conflict',
  422: 'Unprocessable Content',
  500: 'Internal Server Error',
};

/** Field error of the mock API: only codes of the shared catalog, which are all translated. */
export interface MockFieldError extends ApiFieldError {
  readonly code: ApiErrorCode;
}

/** What an error answer may carry besides status and code. */
export interface MockProblemDetails {
  /** Short text for developers and logs (never shown to the user). */
  title: string;
  args?: ApiProblemArgs;
  errors?: readonly MockFieldError[];
}

/**
 * Error answer of the mock API in the backend's format: Problem Details (RFC 9457) extended with
 * `code`, `args`, `errors` and a `traceId` like .NET's (W3C trace context), as in the backend's
 * response standard.
 */
export function problem(
  request: HttpRequest<unknown>,
  status: number,
  code: ApiErrorCode,
  details: MockProblemDetails,
): Observable<never> {
  return throwError(
    () =>
      new HttpErrorResponse({
        status,
        statusText: STATUS_TEXTS[status] ?? 'Error',
        url: request.url,
        error: {
          type: `https://errors.summerbooking/${code.replaceAll('.', '/').replaceAll('_', '-')}`,
          status,
          code,
          ...details,
          traceId: `00-${randomHex(16)}-${randomHex(8)}-01`,
        },
      }),
  ).pipe(delay(150));
}

function randomHex(bytes: number): string {
  return Array.from(crypto.getRandomValues(new Uint8Array(bytes)), (byte) =>
    byte.toString(16).padStart(2, '0'),
  ).join('');
}
