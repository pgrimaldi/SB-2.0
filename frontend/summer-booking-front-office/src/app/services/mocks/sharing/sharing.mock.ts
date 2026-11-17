import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { SharingGeneralInformation } from '../../../entities/settings/sharing/sharing-general-information';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';

let generalInformation: SharingGeneralInformation = {
  isSharingEnabled: true,
  isMinimumBookingDayEnabled: true,
  minimumBookingDayQuantity: 3,
  isOnlySeasonalEnabled: false,
  isEnabledOnAllArticles: true,
  isScheduledAbsenceSharing: true,
  isManualCreditAssignmentEnabled: false,
  resoldDayCreditQuantity: 1,
  manualCreditValue: null,
  isAutomaticVoucherEnabled: false,
  voucherCreditValue: null,
  isPrepaidCardTopUpEnabled: false,
  prepaidCardCreditValue: null,
};

/**
 * `GET /api/sharing/general-information`: the sharing settings of the property. 401 without a valid
 * access token.
 */
export const sharingGeneralInformationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  return of(
    new HttpResponse({ status: 200, url: request.url, body: { ...generalInformation } }),
  ).pipe(delay(150));
};

/**
 * `POST /api/sharing/save-information` with every setting of the page: keeps them (until the page
 * is reloaded). 204 without a body; 401 without a valid access token.
 */
export const saveSharingInformationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<SharingGeneralInformation>;
  generalInformation = onlyFieldsOf(SharingGeneralInformation, {
    ...generalInformation,
    ...body,
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};
