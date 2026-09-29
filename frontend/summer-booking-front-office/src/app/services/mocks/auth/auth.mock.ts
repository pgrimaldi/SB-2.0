import { HttpErrorResponse, HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, from, of, switchMap, throwError } from 'rxjs';
import { AuthSession, AuthUser, SignInRequest } from '../../../entities/auth/credentials';
import { DEMO_PROPERTY } from '../properties/properties.mock';

// Test account of the mock. Only the SHA-256 of the password is kept: the readable password
// never appears in the repository nor in the published JavaScript.
const MOCK_ACCOUNT = {
  username: 'summertest465@gmail.com',
  /** The invented "Lido Demo" property. */
  idProperty: DEMO_PROPERTY.publicId,
  roles: ['Manager'],
  passwordSha256: '42862e8e5e2e0915ad980297cc224059dcc424323ea325a9754839c79bca93f5',
};

/** Token lifetimes of the backend configuration (`Auth:*`), in seconds / days. */
const ACCESS_TOKEN_SECONDS = 15 * 60;
const REFRESH_TOKEN_DAYS = 30;
const REFRESH_TOKEN_REMEMBER_ME_DAYS = 90;

/**
 * MOCK ONLY. The real server keeps the refresh token in an HttpOnly cookie that no script can read or
 * write; an interceptor cannot create such a cookie, so the mock simulates it (and the server's
 * registry of refresh tokens) in browser storage: sessionStorage for a standard session (ends with
 * the browser), localStorage with "remember me". Never used with mocks off (production).
 */
const SIMULATED_COOKIE_KEY = 'sb.mock.refresh-cookie';

interface SimulatedCookie {
  refreshToken: string;
  remember: boolean;
  expiresAt: number;
}

/** Access tokens issued by the mock and when they expire (the server validates the JWT instead). */
const accessTokens = new Map<string, number>();

/** `POST /api/auth/signin`: session and refresh cookie for the test account, 401 otherwise. */
export const signInMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  const {
    username = '',
    password = '',
    remember = false,
  } = (request.body ?? {}) as Partial<SignInRequest>;

  return from(sha256(password)).pipe(
    delay(400),
    switchMap((passwordSha256) => {
      const valid =
        username.trim().toLowerCase() === MOCK_ACCOUNT.username &&
        passwordSha256 === MOCK_ACCOUNT.passwordSha256;
      if (!valid) {
        return unauthorized(request, 'Invalid credentials');
      }
      writeCookie(remember);
      return ok(request, newSession());
    }),
  );
};

/** `POST /api/auth/refresh`: new access token and rotated refresh cookie, 401 without a valid one. */
export const refreshMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  const cookie = readCookie();
  if (!cookie || cookie.expiresAt <= Date.now()) {
    removeCookie();
    return unauthorized(request, 'No valid session');
  }
  writeCookie(cookie.remember);
  return ok(request, newSession());
};

/** `POST /api/auth/logout`: revokes the session of the refresh cookie; 401 when there is none. */
export const logoutMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!readCookie()) {
    return unauthorized(request, 'No valid session');
  }
  removeCookie();
  accessTokens.clear(); // the one test account: its access tokens stop working with the session
  return of(new HttpResponse<void>({ status: 204, url: request.url })).pipe(delay(150));
};

/** True when the request carries a valid access token, as a protected API endpoint requires. */
export function isAuthorized(request: HttpRequest<unknown>): boolean {
  const expiresAt = accessTokens.get(bearer(request) ?? '');
  return expiresAt !== undefined && expiresAt > Date.now();
}

/** 401 answer of a protected endpoint called without a valid access token. */
export function unauthorized(
  request: HttpRequest<unknown>,
  message = 'Invalid or expired access token',
): Observable<never> {
  return throwError(
    () =>
      new HttpErrorResponse({
        status: 401,
        statusText: 'Unauthorized',
        url: request.url,
        error: { message },
      }),
  );
}

function newSession(): AuthSession {
  const accessToken = `mock-access-${randomToken()}`;
  accessTokens.set(accessToken, Date.now() + ACCESS_TOKEN_SECONDS * 1000);
  const user: AuthUser = {
    email: MOCK_ACCOUNT.username,
    idProperty: MOCK_ACCOUNT.idProperty,
    roles: MOCK_ACCOUNT.roles,
  };
  return { accessToken, expiresIn: ACCESS_TOKEN_SECONDS, user };
}

function ok(request: HttpRequest<unknown>, body: AuthSession): Observable<HttpEvent<unknown>> {
  return of(new HttpResponse({ status: 200, url: request.url, body })).pipe(delay(150));
}

function bearer(request: HttpRequest<unknown>): string | null {
  return request.headers.get('Authorization')?.replace(/^Bearer /, '') ?? null;
}

/** Sets a new refresh token (rotation): the previous one is no longer valid. */
function writeCookie(remember: boolean): void {
  removeCookie();
  const days = remember ? REFRESH_TOKEN_REMEMBER_ME_DAYS : REFRESH_TOKEN_DAYS;
  const cookie: SimulatedCookie = {
    refreshToken: randomToken(),
    remember,
    expiresAt: Date.now() + days * 24 * 60 * 60 * 1000,
  };
  storage(remember)?.setItem(SIMULATED_COOKIE_KEY, JSON.stringify(cookie));
}

function readCookie(): SimulatedCookie | null {
  const stored =
    storage(false)?.getItem(SIMULATED_COOKIE_KEY) ?? storage(true)?.getItem(SIMULATED_COOKIE_KEY);
  try {
    return stored ? (JSON.parse(stored) as SimulatedCookie) : null;
  } catch {
    return null;
  }
}

function removeCookie(): void {
  storage(false)?.removeItem(SIMULATED_COOKIE_KEY);
  storage(true)?.removeItem(SIMULATED_COOKIE_KEY);
}

function storage(remember: boolean): Storage | undefined {
  try {
    return remember ? localStorage : sessionStorage;
  } catch {
    return undefined;
  }
}

function randomToken(): string {
  return [...crypto.getRandomValues(new Uint8Array(32))]
    .map((byte) => byte.toString(16).padStart(2, '0'))
    .join('');
}

async function sha256(text: string): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(text));
  return [...new Uint8Array(digest)].map((byte) => byte.toString(16).padStart(2, '0')).join('');
}
