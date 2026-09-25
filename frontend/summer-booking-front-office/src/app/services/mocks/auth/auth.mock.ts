import { HttpErrorResponse, HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, from, of, switchMap, throwError } from 'rxjs';
import { SignInRequest, SignInResponse } from '../../../entities/auth/credentials';

// Test account of the mock. Only the SHA-256 of the password is kept: the readable password
// never appears in the repository nor in the published JavaScript.
const MOCK_ACCOUNT = {
  username: 'summertest465@gmail.com',
  passwordSha256: '42862e8e5e2e0915ad980297cc224059dcc424323ea325a9754839c79bca93f5',
};

const MOCK_TOKEN = 'mock-token-summertest465';

/** `POST /api/auth/signin`: 200 with token and user for the test account, 401 otherwise. */
export const signInMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  const { username = '', password = '' } = (request.body ?? {}) as Partial<SignInRequest>;

  return from(sha256(password)).pipe(
    delay(400),
    switchMap((passwordSha256) => {
      const valid =
        username.trim().toLowerCase() === MOCK_ACCOUNT.username &&
        passwordSha256 === MOCK_ACCOUNT.passwordSha256;

      if (!valid) {
        return throwError(
          () =>
            new HttpErrorResponse({
              status: 401,
              statusText: 'Unauthorized',
              url: request.url,
              error: { message: 'Invalid credentials' },
            }),
        );
      }

      const body: SignInResponse = { token: MOCK_TOKEN, user: { email: MOCK_ACCOUNT.username } };
      return of(new HttpResponse({ status: 200, url: request.url, body }));
    }),
  );
};

async function sha256(text: string): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(text));
  return [...new Uint8Array(digest)].map((byte) => byte.toString(16).padStart(2, '0')).join('');
}
