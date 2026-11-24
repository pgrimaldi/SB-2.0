import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ContactSupportRequest } from '../../../entities/settings/contact-support/contact-support-request';
import { SupportInfo } from '../../../entities/settings/contact-support/support-info';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class SystemService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/system`;

  readonly supportInfo = (): Observable<SupportInfo> =>
    this.httpClient.post<SupportInfo>(`${this.endpoint}/info-support`, null);

  /** The backend sends the message to the support team. Answers 204 with no body. */
  readonly contactSupport = (request: ContactSupportRequest): Observable<void> =>
    this.httpClient.post<void>(
      `${this.endpoint}/contact-support`,
      onlyFieldsOf(ContactSupportRequest, request),
    );
}
