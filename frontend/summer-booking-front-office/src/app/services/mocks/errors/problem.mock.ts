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

/** Only codes of the shared catalog, which are all translated. */
export interface MockFieldError extends ApiFieldError {
  readonly code: ApiErrorCode;
}

export interface MockProblemDetails {
  args?: Readonly<Record<string, ApiProblemArgs[string] | readonly string[] | readonly number[]>>;
  errors?: readonly MockFieldError[];
}

/**
 * Error answer of the mock API in the backend's format (`ProblemDetailsWriter`): Problem Details
 * (RFC 9457) with the code as `title`, `type` from the code, `args` only when there are some, every
 * element of `errors` with its `args` (empty too) and a `traceId` like .NET's (W3C trace context).
 */
export function problem(
  request: HttpRequest<unknown>,
  status: number,
  code: ApiErrorCode,
  { args, errors }: MockProblemDetails = {},
): Observable<never> {
  return throwError(
    () =>
      new HttpErrorResponse({
        status,
        statusText: STATUS_TEXTS[status] ?? 'Error',
        url: request.url,
        error: {
          type: `https://errors.summerbooking/${code.replaceAll('.', '/')}`,
          title: code,
          status,
          code,
          ...(args && Object.keys(args).length ? { args } : {}),
          ...(errors?.length
            ? { errors: errors.map((error) => ({ ...error, args: error.args ?? {} })) }
            : {}),
          traceId: `00-${randomHex(16)}-${randomHex(8)}-01`,
        },
      }),
  ).pipe(delay(150));
}

/** The property is not one the user may work on: the operation is cancelled. */
export function forbidden(request: HttpRequest<unknown>): Observable<never> {
  return problem(request, 403, ApiErrorCode.AuthForbidden);
}

/** A route value the backend does not know (`Results.NotFound()`): 404 without a body. */
export function notFoundWithoutBody(request: HttpRequest<unknown>): Observable<never> {
  return throwError(
    () => new HttpErrorResponse({ status: 404, statusText: STATUS_TEXTS[404], url: request.url }),
  ).pipe(delay(150));
}

/** An article that does not exist, or no longer: the operation is cancelled. */
export function resourceNotFound(request: HttpRequest<unknown>): Observable<never> {
  return problem(request, 404, ApiErrorCode.ResourceNotFound);
}

function randomHex(bytes: number): string {
  return Array.from(crypto.getRandomValues(new Uint8Array(bytes)), (byte) =>
    byte.toString(16).padStart(2, '0'),
  ).join('');
}
