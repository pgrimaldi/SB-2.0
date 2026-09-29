import { HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Observable, firstValueFrom } from 'rxjs';
import { AuthService } from '../../api/auth/auth.service';
import { mockApiInterceptor } from '../mock-api.interceptor';
import { DEMO_PROPERTY } from '../properties/properties.mock';

describe('auth mock', () => {
  const TEST_PASSWORD_SHA256 = '42862e8e5e2e0915ad980297cc224059dcc424323ea325a9754839c79bca93f5';
  const USERNAME = 'summertest465@gmail.com';

  const service = () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([mockApiInterceptor]))],
    });
    return TestBed.inject(AuthService);
  };

  /** The readable password is not in the code: the hash of the typed one is made to match. */
  const acceptAnyPassword = () => {
    const bytes = TEST_PASSWORD_SHA256.match(/../g)!.map((hex) => parseInt(hex, 16));
    vi.spyOn(crypto.subtle, 'digest').mockResolvedValue(new Uint8Array(bytes).buffer);
  };

  /** Status of the error answer, or 0 when the request succeeded. */
  const status = (request: Observable<unknown>) =>
    firstValueFrom(request).then(
      () => 0,
      (error: HttpErrorResponse) => error.status,
    );

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('should sign in the test account with user data and a short-lived access token', async () => {
    acceptAnyPassword();

    const session = await firstValueFrom(
      service().signIn({
        username: ' SummerTest465@gmail.com ',
        password: 'typed',
        remember: false,
      }),
    );

    expect(session.user).toEqual({
      email: USERNAME,
      idProperty: DEMO_PROPERTY.publicId,
      roles: ['Manager'],
    });
    expect(session.accessToken).toBeTruthy();
    expect(session.expiresIn).toBe(900);
  });

  it('should refuse wrong credentials with 401', async () => {
    const auth = service();

    expect(
      await status(auth.signIn({ username: USERNAME, password: 'wrong', remember: false })),
    ).toBe(401);
  });

  it('should renew the access token from the (simulated) refresh cookie until logout', async () => {
    acceptAnyPassword();
    const auth = service();
    const first = await firstValueFrom(
      auth.signIn({ username: USERNAME, password: 'typed', remember: true }),
    );

    const second = await firstValueFrom(auth.refresh());
    expect(second.accessToken).not.toBe(first.accessToken);

    await firstValueFrom(auth.logout());
    expect(await status(auth.refresh())).toBe(401);
  });

  it('should refuse refresh and logout without a sign-in', async () => {
    const auth = service();

    expect(await status(auth.refresh())).toBe(401);
    expect(await status(auth.logout())).toBe(401);
  });
});
