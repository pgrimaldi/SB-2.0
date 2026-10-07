import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ContactSupportRequest } from '../../../entities/settings/contact-support/contact-support-request';
import { SupportInfo } from '../../../entities/settings/contact-support/support-info';
import { SupportInfoRequest } from '../../../entities/settings/contact-support/support-info-request';
import { EmailConfigurationData } from '../../../entities/settings/email-configuration/email-configuration-data';
import { EmailConfigurationDataRequest } from '../../../entities/settings/email-configuration/email-configuration-data-request';
import { EmailConfigurationRequest } from '../../../entities/settings/email-configuration/email-configuration-request';

@Injectable({ providedIn: 'root' })
export class SystemService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/system`;

  readonly supportInfo = (request: SupportInfoRequest): Observable<SupportInfo> =>
    this.httpClient.post<SupportInfo>(`${this.endpoint}/info-support`, request);

  /** GET: the property travels in the query string (`?idProperty=…`). */
  readonly emailConfiguration = (
    request: EmailConfigurationDataRequest,
  ): Observable<EmailConfigurationData> =>
    this.httpClient.get<EmailConfigurationData>(`${this.endpoint}/email-configuration`, {
      params: { idProperty: request.idProperty },
    });

  /** The backend sends a test email with these settings. Answers 204 with no body. */
  readonly sendTestEmail = (request: EmailConfigurationRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/send-test-email`, request);

  /** Answers 204 with no body. */
  readonly saveEmailConfiguration = (request: EmailConfigurationRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/save-email-configuration`, request);

  /** The backend sends the message to the support team. Answers 204 with no body. */
  readonly contactSupport = (request: ContactSupportRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/contact-support`, request);
}
