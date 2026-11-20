import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { OnlineBookingServicesOptions } from '../../../entities/settings/online-booking-configuration/online-booking-services-options';
import { OnlineBookingServicesOptionsUpdateRequest } from '../../../entities/settings/online-booking-configuration/online-booking-services-options-update-request';
import { onlyFieldsOf } from '../only-fields-of';
import { ifMatch, versionOf } from '../versions/version-headers';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/booking`;

  readonly onlineServicesOptions = (): Observable<OnlineBookingServicesOptions> =>
    this.httpClient.get<OnlineBookingServicesOptions>(
      `${this.endpoint}/get-online-services-options`,
    );

  /** Every option, changed or not. Answers 204 with no body and the new version. */
  readonly updateOnlineServicesOptions = (
    options: OnlineBookingServicesOptionsUpdateRequest,
    version: string | null,
  ): Observable<string | null> =>
    this.httpClient
      .post<void>(
        `${this.endpoint}/update-online-services-options`,
        onlyFieldsOf(OnlineBookingServicesOptionsUpdateRequest, options),
        { headers: ifMatch(version), observe: 'response' },
      )
      .pipe(map(versionOf));
}
