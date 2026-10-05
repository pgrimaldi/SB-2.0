import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ContactSupportRequest } from '../../../entities/system/contact-support-request';

@Injectable({ providedIn: 'root' })
export class SystemService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/system`;

  /** The backend sends the message to the support team. Answers 204 with no body. */
  readonly contactSupport = (request: ContactSupportRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/contact-support`, request);
}
