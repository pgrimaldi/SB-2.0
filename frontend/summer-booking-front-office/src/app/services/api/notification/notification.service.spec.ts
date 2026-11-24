import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { SmtpSecurity } from '../../../entities/enums/smtp-security';
import { ContactSupportRequest } from '../../../entities/settings/contact-support/contact-support-request';
import { EmailConfigurationRequest } from '../../../entities/settings/email-configuration/email-configuration-request';
import { NotificationService } from './notification.service';

describe('NotificationService', () => {
  const REQUEST: EmailConfigurationRequest = {
    ...new EmailConfigurationRequest(),
    fromEmail: 'prenotazioni@lido-demo.example',
    smtpPort: 587,
    security: SmtpSecurity.StartTls,
    testRecipients: ['prova@lido-demo.example'],
  };

  const setup = () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    return {
      notification: TestBed.inject(NotificationService),
      controller: TestBed.inject(HttpTestingController),
    };
  };

  it('should save the email configuration with the version read in If-Match and give back the new one', () => {
    const { notification, controller } = setup();
    let version: string | null | undefined;

    notification.saveEmailConfiguration(REQUEST, 'v1').subscribe((saved) => (version = saved));

    const post = controller.expectOne({
      method: 'POST',
      url: `${environment.apiBaseUrl}/notification/save-email-configuration`,
    });
    expect(post.request.body).toEqual(REQUEST);
    expect(post.request.headers.get('If-Match')).toBe('"v1"');
    post.flush(null, { status: 204, statusText: 'No Content', headers: { ETag: '"v2"' } });
    expect(version).toBe('v2');
    controller.verify();
  });

  it('should read the configuration and send the test email under the domain of the notifications', () => {
    const { notification, controller } = setup();

    notification.emailConfiguration().subscribe();
    notification.sendTestEmail(REQUEST).subscribe();

    controller.expectOne({
      method: 'GET',
      url: `${environment.apiBaseUrl}/notification/email-configuration`,
    });
    const test = controller.expectOne({
      method: 'POST',
      url: `${environment.apiBaseUrl}/notification/send-test-email`,
    });
    expect(test.request.body).toEqual(REQUEST);
    expect(test.request.headers.has('If-Match')).toBe(false);
    controller.verify();
  });

  it('should read the support contacts and send the message to the support', () => {
    const { notification, controller } = setup();
    const contact: ContactSupportRequest = {
      ...new ContactSupportRequest(),
      firstName: 'Anna',
      message: 'Vorrei informazioni.',
    };

    notification.supportInfo().subscribe();
    notification.contactSupport(contact).subscribe();

    controller.expectOne({
      method: 'GET',
      url: `${environment.apiBaseUrl}/notification/support/info`,
    });
    const post = controller.expectOne({
      method: 'POST',
      url: `${environment.apiBaseUrl}/notification/support/contact`,
    });
    expect(post.request.body).toEqual(contact);
    controller.verify();
  });
});
