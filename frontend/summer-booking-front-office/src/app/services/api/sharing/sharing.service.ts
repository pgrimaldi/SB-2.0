import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SharingGeneralInformation } from '../../../entities/settings/sharing/sharing-general-information';
import { SharingSaveInformationRequest } from '../../../entities/settings/sharing/sharing-save-information-request';
import { onlyFieldsOf } from '../only-fields-of';
import { ifMatch, versionOf } from '../versions/version-headers';

@Injectable({ providedIn: 'root' })
export class SharingService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/sharing`;

  readonly generalInformation = (): Observable<SharingGeneralInformation> =>
    this.httpClient.get<SharingGeneralInformation>(`${this.endpoint}/general-information`);

  /** Every setting of the page, changed or not. Answers 204 with no body and the new version. */
  readonly saveInformation = (
    information: SharingSaveInformationRequest,
    version: string | null,
  ): Observable<string | null> =>
    this.httpClient
      .post<void>(
        `${this.endpoint}/save-information`,
        onlyFieldsOf(SharingSaveInformationRequest, information),
        { headers: ifMatch(version), observe: 'response' },
      )
      .pipe(map(versionOf));
}
