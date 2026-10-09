import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, from, map, of, switchMap, throwError } from 'rxjs';

/** A Problem Details is small: anything bigger is not one. */
const MAX_ERROR_SIZE = 64 * 1024;

/**
 * With `responseType: 'blob'` Angular gives the body of an error as a Blob too: it is read back as
 * JSON, so that `toApiProblem` finds the backend's `code`. Use it with `catchError`.
 */
export function readBlobError(error: unknown): Observable<never> {
  if (!(error instanceof HttpErrorResponse) || !(error.error instanceof Blob)) {
    return throwError(() => error);
  }
  const body: Observable<unknown> =
    error.error.size > MAX_ERROR_SIZE
      ? of(null)
      : from(error.error.text()).pipe(
          map((text): unknown => JSON.parse(text)),
          catchError(() => of(null)),
        );
  return body.pipe(
    switchMap((parsed) =>
      throwError(
        () =>
          new HttpErrorResponse({
            error: parsed,
            headers: error.headers,
            status: error.status,
            statusText: error.statusText,
            url: error.url ?? undefined,
          }),
      ),
    ),
  );
}
