import {
  HttpErrorResponse,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthBehaviour } from '../../behaviours/auth/auth.behaviour';
import { isApiUrl } from './api-url';

/**
 * Requests that work with the refresh cookie, not with the access token: they never carry it and a
 * 401 there is never followed by a refresh (on logout it could reopen the session being closed).
 */
const COOKIE_ENDPOINTS = [
  `${environment.apiBaseUrl}/auth/signin`,
  `${environment.apiBaseUrl}/auth/refresh`,
  `${environment.apiBaseUrl}/auth/logout`,
];

/**
 * Sends the access token (`Authorization: Bearer`) to our API only, never to other hosts. When the
 * API answers 401 (token expired), gets a new one from the refresh cookie and repeats the request
 * once; if the session is over, the user is signed out. A request of a session that has changed hands
 * since it was sent (sign-out, another user) is never renewed nor repeated: it would run under a
 * session that did not ask for it.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isApiUrl(request.url) || COOKIE_ENDPOINTS.includes(request.url)) {
    return next(request);
  }
  const auth = inject(AuthBehaviour);
  const token = auth.token();
  if (!token) {
    return next(request);
  }
  const generation = auth.sessionGeneration();

  return send(request, token, next).pipe(
    catchError((error: unknown) => {
      if (
        !(error instanceof HttpErrorResponse) ||
        error.status !== 401 ||
        auth.sessionGeneration() !== generation
      ) {
        return throwError(() => error);
      }
      return auth.refresh().pipe(
        switchMap((fresh) => {
          if (!fresh) {
            // Signed out only when no session is left: an old answer must not close a newer one.
            if (!auth.isAuthenticated()) {
              auth.expire();
            }
            return throwError(() => error);
          }
          return send(request, fresh, next);
        }),
      );
    }),
  );
};

function send(request: HttpRequest<unknown>, token: string, next: HttpHandlerFn) {
  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
}
