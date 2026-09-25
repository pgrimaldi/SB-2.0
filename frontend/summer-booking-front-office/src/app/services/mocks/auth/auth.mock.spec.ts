import { HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../api/auth/auth.service';
import { mockApiInterceptor } from '../mock-api.interceptor';

describe('signInMock', () => {
  const TEST_PASSWORD_SHA256 = '42862e8e5e2e0915ad980297cc224059dcc424323ea325a9754839c79bca93f5';

  const signIn = (username: string, password: string) => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([mockApiInterceptor]))],
    });
    return firstValueFrom(TestBed.inject(AuthService).signIn({ username, password }));
  };

  afterEach(() => vi.restoreAllMocks());

  it('should sign in the test account', async () => {
    // The readable password is not in the code: the hash of the typed one is made to match.
    const bytes = TEST_PASSWORD_SHA256.match(/../g)!.map((hex) => parseInt(hex, 16));
    vi.spyOn(crypto.subtle, 'digest').mockResolvedValue(new Uint8Array(bytes).buffer);

    const response = await signIn(' SummerTest465@gmail.com ', 'typed password');

    expect(response.user.email).toBe('summertest465@gmail.com');
    expect(response.token).toBeTruthy();
  });

  it('should refuse wrong credentials with 401', async () => {
    const error = await signIn('summertest465@gmail.com', 'wrong').catch((e: unknown) => e);

    expect(error).toBeInstanceOf(HttpErrorResponse);
    expect((error as HttpErrorResponse).status).toBe(401);
  });
});
