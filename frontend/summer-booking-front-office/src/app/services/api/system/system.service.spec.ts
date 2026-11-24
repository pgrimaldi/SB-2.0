import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { EmailConfigurationRequest } from '../../../entities/settings/email-configuration/email-configuration-request';
import { SystemService } from './system.service';

describe('SystemService', () => {
  it('should save the email configuration with the version read in If-Match and give back the new one', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const system = TestBed.inject(SystemService);
    const controller = TestBed.inject(HttpTestingController);
    const configuration = Object.assign(new EmailConfigurationRequest(), {
      senderMailAddress: 'prenotazioni@lido-demo.example',
      smtpPort: 587,
      smtpSecurity: 'Tls' as const,
    });
    let version: string | null | undefined;

    system.saveEmailConfiguration(configuration, 'v1').subscribe((saved) => (version = saved));

    const post = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/system/save-email-configuration`,
    );
    expect(post.request.body).toEqual({
      ...new EmailConfigurationRequest(),
      senderMailAddress: 'prenotazioni@lido-demo.example',
      smtpPort: 587,
      smtpSecurity: 'Tls',
    });
    expect(post.request.headers.get('If-Match')).toBe('"v1"');
    post.flush(null, { status: 204, statusText: 'No Content', headers: { ETag: '"v2"' } });
    expect(version).toBe('v2');
    controller.verify();
  });
});
