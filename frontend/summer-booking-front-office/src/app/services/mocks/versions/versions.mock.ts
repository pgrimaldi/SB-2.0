import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { etag, ifMatchOf } from '../../api/versions/version-headers';
import { problem } from '../errors/problem.mock';

/** The version of a group of data, as a backend could compute it: a hash of its values. */
export function versionOf(data: unknown): string {
  let hash = 0x811c9dc5;
  for (const char of JSON.stringify(data)) {
    hash = Math.imul(hash ^ char.charCodeAt(0), 0x01000193) >>> 0;
  }
  return hash.toString(16).padStart(8, '0');
}

/** 409 `resource.version_conflict` when the `If-Match` of the save is not the version of `current`. */
export function versionConflict(
  request: HttpRequest<unknown>,
  current: unknown,
): Observable<never> | null {
  return ifMatchOf(request.headers) === versionOf(current)
    ? null
    : problem(request, 409, ApiErrorCode.ResourceVersionConflict);
}

/** The answer of a save, with the new version of `saved` in its `ETag`. */
export function savedWithVersion<T>(
  request: HttpRequest<unknown>,
  saved: unknown,
  body: T | null = null,
  status = body === null ? 204 : 200,
): Observable<HttpEvent<unknown>> {
  return of(
    new HttpResponse({ status, url: request.url, body, headers: etag(versionOf(saved)) }),
  ).pipe(delay(600));
}
