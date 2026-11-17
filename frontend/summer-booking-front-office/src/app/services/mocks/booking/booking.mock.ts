import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { OnlineBookingServicesOptions } from '../../../entities/settings/online-booking-configuration/online-booking-services-options';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';

let onlineServicesOptions = new OnlineBookingServicesOptions();

/**
 * `GET /api/booking/get-online-services-options`: the options of the online booking of services;
 * all off and empty until saved. 401 without a valid access token.
 */
export const onlineServicesOptionsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  return of(
    new HttpResponse({ status: 200, url: request.url, body: { ...onlineServicesOptions } }),
  ).pipe(delay(300));
};

/**
 * `POST /api/booking/update-online-services-options` with every option: keeps them (until the page
 * is reloaded). 204 without a body; 401 without a valid access token.
 */
export const updateOnlineServicesOptionsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<OnlineBookingServicesOptions>;
  onlineServicesOptions = onlyFieldsOf(OnlineBookingServicesOptions, {
    ...onlineServicesOptions,
    ...body,
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};
