import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ManagementRequest } from '../../../entities/management/management-request';
import { ContactSupportRequest } from '../../../entities/system/contact-support-request';
import { EmailConfigurationData } from '../../../entities/system/email-configuration-data';
import { EmailConfigurationRequest } from '../../../entities/system/email-configuration-request';
import { SupportInfo } from '../../../entities/system/support-info';

@Injectable({ providedIn: 'root' })
export class SystemService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/system`;

  readonly supportInfo = (
    request: Pick<ManagementRequest, 'idProperty'>,
  ): Observable<SupportInfo> =>
    this.httpClient.post<SupportInfo>(`${this.endpoint}/info-support`, request);

  /** GET: the property travels in the query string (`?idProperty=…`). */
  readonly emailConfiguration = (
    request: Pick<ManagementRequest, 'idProperty'>,
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
