import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { OnlineBookingServicesOptions } from '../../../entities/settings/online-booking-configuration/online-booking-services-options';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/booking`;

  /** GET: the property travels in the query string (`?idProperty=…`). */
  readonly onlineServicesOptions = (idProperty: string): Observable<OnlineBookingServicesOptions> =>
    this.httpClient.get<OnlineBookingServicesOptions>(
      `${this.endpoint}/get-online-services-options`,
      { params: { idProperty } },
    );

  /** Every option, changed or not. Answers 204 with no body. */
  readonly updateOnlineServicesOptions = (
    idProperty: string,
    options: OnlineBookingServicesOptions,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-online-services-options`, {
      ...onlyFieldsOf(OnlineBookingServicesOptions, options),
      idProperty,
    });
}
