import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { OnlineBookingServicesOptions } from '../../../entities/settings/online-booking-configuration/online-booking-services-options';
import { OnlineBookingServicesOptionsUpdateRequest } from '../../../entities/settings/online-booking-configuration/online-booking-services-options-update-request';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { savedWithVersion, versionConflict, versionOf } from '../versions/versions.mock';

let onlineServicesOptions = new OnlineBookingServicesOptionsUpdateRequest();

/**
 * `GET /api/booking/get-online-services-options`: the options of the online booking of services
 * and their version; all off and empty until saved. 401 without a valid access token.
 */
export const onlineServicesOptionsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const options: OnlineBookingServicesOptions = {
    ...onlineServicesOptions,
    version: versionOf(onlineServicesOptions),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: options })).pipe(delay(300));
};

/**
 * `POST /api/booking/update-online-services-options` with every option and the version read in
 * `If-Match`: keeps them (until the page is reloaded). 204 without a body, the new version in
 * `ETag`; 401 without a valid access token; 409 `resource.version_conflict` when the options
 * changed after the read.
 */
export const updateOnlineServicesOptionsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const conflict = versionConflict(request, onlineServicesOptions);
  if (conflict) {
    return conflict;
  }
  const body = (request.body ?? {}) as Partial<OnlineBookingServicesOptionsUpdateRequest>;
  onlineServicesOptions = onlyFieldsOf(OnlineBookingServicesOptionsUpdateRequest, {
    ...onlineServicesOptions,
    ...body,
  });
  return savedWithVersion(request, onlineServicesOptions);
};
