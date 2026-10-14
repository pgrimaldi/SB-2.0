import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SharingGeneralInformation } from '../../../entities/settings/sharing/sharing-general-information';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class SharingService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/sharing`;

  readonly generalInformation = (idProperty: string): Observable<SharingGeneralInformation> =>
    this.httpClient.get<SharingGeneralInformation>(`${this.endpoint}/general-information`, {
      params: { idProperty },
    });

  /** Every setting of the page, changed or not. Answers 204 with no body. */
  readonly saveInformation = (
    idProperty: string,
    information: SharingGeneralInformation,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/save-information`, {
      ...onlyFieldsOf(SharingGeneralInformation, information),
      idProperty,
    });
}
