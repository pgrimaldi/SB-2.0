import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { SharingGeneralInformation } from '../../../entities/settings/sharing/sharing-general-information';
import { SharingSaveInformationRequest } from '../../../entities/settings/sharing/sharing-save-information-request';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { savedWithVersion, versionConflict, versionOf } from '../versions/versions.mock';

let generalInformation: SharingSaveInformationRequest = {
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
 * `GET /api/sharing/general-information`: the sharing settings of the property and their version.
 * 401 without a valid access token.
 */
export const sharingGeneralInformationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const information: SharingGeneralInformation = {
    ...generalInformation,
    version: versionOf(generalInformation),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: information })).pipe(
    delay(150),
  );
};

/**
 * `POST /api/sharing/save-information` with every setting of the page and the version read in
 * `If-Match`: keeps them (until the page is reloaded). 204 without a body, the new version in
 * `ETag`; 401 without a valid access token; 409 `resource.version_conflict` when the settings
 * changed after the read.
 */
export const saveSharingInformationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const conflict = versionConflict(request, generalInformation);
  if (conflict) {
    return conflict;
  }
  const body = (request.body ?? {}) as Partial<SharingSaveInformationRequest>;
  generalInformation = onlyFieldsOf(SharingSaveInformationRequest, {
    ...generalInformation,
    ...body,
  });
  return savedWithVersion(request, generalInformation);
};
