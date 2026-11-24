import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ContactSupportRequest } from '../../../entities/settings/contact-support/contact-support-request';
import { SupportInfo } from '../../../entities/settings/contact-support/support-info';
import { EmailConfigurationData } from '../../../entities/settings/email-configuration/email-configuration-data';
import { EmailConfigurationRequest } from '../../../entities/settings/email-configuration/email-configuration-request';
import { onlyFieldsOf } from '../only-fields-of';
import { ifMatch, versionOf } from '../versions/version-headers';

@Injectable({ providedIn: 'root' })
export class SystemService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/system`;

  readonly supportInfo = (): Observable<SupportInfo> =>
    this.httpClient.post<SupportInfo>(`${this.endpoint}/info-support`, null);

  readonly emailConfiguration = (): Observable<EmailConfigurationData> =>
    this.httpClient.get<EmailConfigurationData>(`${this.endpoint}/email-configuration`);

  /** The backend sends a test email with these settings. Answers 204 with no body. */
  readonly sendTestEmail = (request: EmailConfigurationRequest): Observable<void> =>
    this.httpClient.post<void>(
      `${this.endpoint}/send-test-email`,
      onlyFieldsOf(EmailConfigurationRequest, request),
    );

  /** Answers 204 with no body and the new version. */
  readonly saveEmailConfiguration = (
    request: EmailConfigurationRequest,
    version: string | null,
  ): Observable<string | null> =>
    this.httpClient
      .post<void>(
        `${this.endpoint}/save-email-configuration`,
        onlyFieldsOf(EmailConfigurationRequest, request),
        { headers: ifMatch(version), observe: 'response' },
      )
      .pipe(map(versionOf));

  /** The backend sends the message to the support team. Answers 204 with no body. */
  readonly contactSupport = (request: ContactSupportRequest): Observable<void> =>
    this.httpClient.post<void>(
      `${this.endpoint}/contact-support`,
      onlyFieldsOf(ContactSupportRequest, request),
    );
}
