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

  it('should answer the support contacts, the hours in Italian without an English Accept-Language', async () => {
    const system = await service();

    expect(
      await firstValueFrom(system.supportInfo({ idProperty: DEMO_PROPERTY.publicId })),
    ).toEqual({
      phoneNumber: '050 7916620',
      mailAddress: 'info@summerbooking.it',
      supportHour: [expect.stringContaining('1 maggio'), expect.stringContaining('1 ottobre')],
    });
  });

  it('should refuse the support contacts of an unknown property (400)', async () => {
    const invalid = await firstValueFrom(
      (await service()).supportInfo({ idProperty: 'unknown' }),
    ).catch((failure: HttpErrorResponse) => failure);

    expect((invalid as HttpErrorResponse).status).toBe(400);
  });

  it('should answer the email configuration of the property through GET (query string)', async () => {
    const configuration = await firstValueFrom(
      (await service()).emailConfiguration({ idProperty: DEMO_PROPERTY.publicId }),
    );

    expect(Object.keys(configuration)).toEqual([
      'senderMailAddress',
      'senderName',
      'smtpServerAddress',
      'smtpPort',
      'smtpUsername',
      'smtpPassword',
      'smtpSecurity',
    ]);
    expect(configuration.smtpPort).toBe(587);
    expect(configuration.smtpSecurity).toBe('Tls');
  });

  it('should refuse the email configuration of an unknown property (400)', async () => {
    const invalid = await firstValueFrom(
      (await service()).emailConfiguration({ idProperty: 'unknown' }),
    ).catch((failure: HttpErrorResponse) => failure);

    expect((invalid as HttpErrorResponse).status).toBe(400);
  });

  it('should accept the test email with complete settings (204) and refuse wrong ones (400)', async () => {
    const system = await service();
    const settings = {
      idProperty: DEMO_PROPERTY.publicId,
      senderMailAddress: 'prenotazioni@lido-demo.example',
      senderName: 'Lido Demo',
      smtpServerAddress: 'smtp.lido-demo.example',
      smtpPort: 587,
      smtpUsername: 'prenotazioni@lido-demo.example',
      smtpPassword: 'password-di-esempio',
      smtpSecurity: 'Tls' as const,
    };

    expect(await firstValueFrom(system.sendTestEmail(settings))).toBeNull();
    const invalid = await firstValueFrom(
      system.sendTestEmail({ ...settings, smtpServerAddress: ' ', smtpPort: null }),
    ).catch((failure: HttpErrorResponse) => failure);
    expect((invalid as HttpErrorResponse).status).toBe(400);
    expect((invalid as HttpErrorResponse).error).toEqual(
      expect.objectContaining({
        errors: [
          { field: 'smtpServerAddress', code: 'validation.invalid_value' },
          { field: 'smtpPort', code: 'validation.invalid_value' },
        ],
      }),
    );
  });

  it('should save the email configuration, which the next reading answers (204)', async () => {
    const system = await service();
    const before = await firstValueFrom(
      system.emailConfiguration({ idProperty: DEMO_PROPERTY.publicId }),
    );

    const answer = await firstValueFrom(
      system.saveEmailConfiguration({
        idProperty: DEMO_PROPERTY.publicId,
        ...before,
        senderName: 'Lido Salvato',
        smtpPort: 465,
        smtpSecurity: 'Ssl',
      }),
    );
    const after = await firstValueFrom(
      system.emailConfiguration({ idProperty: DEMO_PROPERTY.publicId }),
    );

    expect(answer).toBeNull();
    expect(after).toEqual({
      ...before,
      senderName: 'Lido Salvato',
      smtpPort: 465,
      smtpSecurity: 'Ssl',
    });
  });

  it('should refuse to save a wrong configuration (400)', async () => {
    const invalid = await firstValueFrom(
      (await service()).saveEmailConfiguration({
        idProperty: DEMO_PROPERTY.publicId,
        senderMailAddress: '',
        senderName: 'Lido Demo',
        smtpServerAddress: 'smtp.lido-demo.example',
        smtpPort: 70000,
        smtpUsername: 'utente',
        smtpPassword: 'password-di-esempio',
        smtpSecurity: null,
      }),
    ).catch((failure: HttpErrorResponse) => failure);

    expect((invalid as HttpErrorResponse).status).toBe(400);
    expect((invalid as HttpErrorResponse).error).toEqual(
      expect.objectContaining({
        errors: [
          { field: 'senderMailAddress', code: 'validation.invalid_value' },
          { field: 'smtpPort', code: 'validation.invalid_value' },
          { field: 'smtpSecurity', code: 'validation.invalid_value' },
        ],
      }),
    );
  });
});
