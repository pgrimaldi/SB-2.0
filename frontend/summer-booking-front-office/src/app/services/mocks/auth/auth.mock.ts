import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, from, of, switchMap } from 'rxjs';
import { AuthSession, AuthUser, LoginRequest } from '../../../entities/auth/credentials';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { problem } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';

// Test account of the mock. Only the SHA-256 of the password is kept: the readable password
// never appears in the repository nor in the published JavaScript.
const MOCK_ACCOUNT = {
  id: '0f3c2a1b-7d6e-4f5a-9b8c-1d2e3f4a5b6c',
  username: 'summertest465@gmail.com',
  propertyId: DEMO_PROPERTY.publicId,
  role: 'manager',
  passwordSha256: '42862e8e5e2e0915ad980297cc224059dcc424323ea325a9754839c79bca93f5',
};

/** As in the backend configuration (`Auth:*`). */
const ACCESS_TOKEN_SECONDS = 10080 * 60;
const REFRESH_TOKEN_DAYS = 30;
const REFRESH_TOKEN_REMEMBER_ME_DAYS = 90;

/**
 * MOCK ONLY. The real server keeps the refresh token in an HttpOnly cookie that no script can read or
 * write; an interceptor cannot create such a cookie, so the mock simulates it (and the server's
 * registry of refresh tokens) in browser storage: sessionStorage for a standard session (ends with
 * the browser), localStorage with "remember me". Never used with mocks off (production).
 */
const SIMULATED_COOKIE_KEY = 'sb.mock.sb_rt';

interface SimulatedCookie {
  refreshToken: string;
  remember: boolean;
  expiresAt: number;
}

/** Expiry of each issued access token; the real server validates the JWT instead. */
const accessTokens = new Map<string, number>();

/** `POST /api/identity/auth/login`: session and refresh cookie for the test account, 401 otherwise. */
export const loginMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  const { username, password, remember = false } = (request.body ?? {}) as Partial<LoginRequest>;

  return from(sha256(password ?? '')).pipe(
    delay(400),
    switchMap((passwordSha256) => {
      const valid =
        username?.trim().toLowerCase() === MOCK_ACCOUNT.username &&
        passwordSha256 === MOCK_ACCOUNT.passwordSha256;
      if (!valid) {
        return unauthorized(request, 'auth.invalid_credentials', 'Invalid credentials');
      }
      writeCookie(remember);
      return ok(request, newSession());
    }),
  );
};

/** `POST /api/identity/auth/refresh`: new access token and rotated refresh cookie, 401 without a valid one. */
export const refreshMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  const cookie = readCookie();
  if (!cookie || cookie.expiresAt <= Date.now()) {
    removeCookie();
    return unauthorized(request, 'auth.session_expired', 'No valid session');
  }
  writeCookie(cookie.remember);
  return ok(request, newSession());
};

/** `POST /api/identity/auth/logout`: revokes the session of the refresh cookie; 401 when there is none. */
export const logoutMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!readCookie()) {
    return unauthorized(request, 'auth.session_expired', 'No valid session');
  }
  removeCookie();
  accessTokens.clear(); // the one test account: its access tokens stop working with the session
  return of(new HttpResponse<void>({ status: 204, url: request.url })).pipe(delay(150));
};

export function isAuthorized(request: HttpRequest<unknown>): boolean {
  const expiresAt = accessTokens.get(bearer(request) ?? '');
  return expiresAt !== undefined && expiresAt > Date.now();
}

export function unauthorized(
  request: HttpRequest<unknown>,
  code: UnauthorizedCode = 'auth.unauthenticated',
  title = 'Invalid or expired access token',
): Observable<never> {
  return problem(request, 401, code, { title });
}

type UnauthorizedCode = Extract<ApiErrorCode, `auth.${string}`>;

function newSession(): AuthSession {
  const accessToken = `mock-access-${randomToken()}`;
  accessTokens.set(accessToken, Date.now() + ACCESS_TOKEN_SECONDS * 1000);
  const user: AuthUser = {
    id: MOCK_ACCOUNT.id,
    email: MOCK_ACCOUNT.username,
    role: MOCK_ACCOUNT.role,
    propertyId: MOCK_ACCOUNT.propertyId,
  };
  return { accessToken, expiresIn: ACCESS_TOKEN_SECONDS, user };
}

function ok(request: HttpRequest<unknown>, body: AuthSession): Observable<HttpEvent<unknown>> {
  return of(new HttpResponse({ status: 200, url: request.url, body })).pipe(delay(150));
}

function bearer(request: HttpRequest<unknown>): string | null {
  return request.headers.get('Authorization')?.replace(/^Bearer /, '') ?? null;
}

/** Rotation: the previous refresh token is no longer valid. */
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
    return;
  }
}

function randomToken(): string {
  return [...crypto.getRandomValues(new Uint8Array(32))]
    .map((byte) => byte.toString(16).padStart(2, '0'))
    .join('');
}

/** The password of the signed-in test account, as the server would check it for an operation. */
export async function isMockPassword(password: unknown): Promise<boolean> {
  return typeof password === 'string' && (await sha256(password)) === MOCK_ACCOUNT.passwordSha256;
}

async function sha256(text: string): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(text));
  return [...new Uint8Array(digest)].map((byte) => byte.toString(16).padStart(2, '0')).join('');
}
