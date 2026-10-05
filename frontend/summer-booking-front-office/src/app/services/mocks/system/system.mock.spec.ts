import { HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { AuthService } from '../../api/auth/auth.service';
import { authInterceptor } from '../../api/auth.interceptor';
import { SystemService } from '../../api/system/system.service';
import { mockApiInterceptor } from '../mock-api.interceptor';
import { DEMO_PROPERTY } from '../properties/properties.mock';

describe('contactSupportMock', () => {
  const TEST_PASSWORD_SHA256 = '42862e8e5e2e0915ad980297cc224059dcc424323ea325a9754839c79bca93f5';
  const CONTACT = {
    idProperty: DEMO_PROPERTY.publicId,
    firstName: 'Anna',
    lastName: 'Bianchi',
    email: 'anna.bianchi@example.com',
    mobilePhone: null,
    message: 'Vorrei informazioni sul listino.',
  };

  const service = async (signedIn = true) => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([authInterceptor, mockApiInterceptor]))],
    });
    if (signedIn) {
      const bytes = TEST_PASSWORD_SHA256.match(/../g)!.map((hex) => parseInt(hex, 16));
      vi.spyOn(crypto.subtle, 'digest').mockResolvedValue(new Uint8Array(bytes).buffer);
      const session = await firstValueFrom(
        TestBed.inject(AuthService).signIn({
          username: 'summertest465@gmail.com',
          password: 'typed',
          remember: false,
        }),
      );
      TestBed.inject(AuthBehaviour).start(session, false);
    }
    return TestBed.inject(SystemService);
  };

  afterEach(() => {
    vi.restoreAllMocks();
    sessionStorage.clear();
  });

  it('should accept the message, the mobile phone being optional (204)', async () => {
    const system = await service();

    expect(await firstValueFrom(system.contactSupport(CONTACT))).toBeNull();
    expect(
      await firstValueFrom(system.contactSupport({ ...CONTACT, mobilePhone: '3331234567' })),
    ).toBeNull();
  });

  it('should refuse missing fields (400) and a missing access token (401)', async () => {
    const invalid = await firstValueFrom(
      (await service()).contactSupport({ ...CONTACT, firstName: ' ', message: '' }),
    ).catch((failure: HttpErrorResponse) => failure);
    TestBed.resetTestingModule();
    const unauthorized = await firstValueFrom((await service(false)).contactSupport(CONTACT)).catch(
      (failure: HttpErrorResponse) => failure,
    );

    expect((invalid as HttpErrorResponse).status).toBe(400);
    expect((invalid as HttpErrorResponse).error).toEqual(
      expect.objectContaining({
        errors: [
          { field: 'firstName', code: 'validation.invalid_value' },
          { field: 'message', code: 'validation.invalid_value' },
        ],
      }),
    );
    expect((unauthorized as HttpErrorResponse).status).toBe(401);
  });
});
