import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { SharingGeneralInformation } from '../../../entities/settings/sharing/sharing-general-information';
import {
  MAX_SHARING_CREDIT_VALUE,
  MAX_SHARING_QUANTITY,
  SharingSaveInformationRequest,
} from '../../../entities/settings/sharing/sharing-save-information-request';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';
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
  manualCreditValue: 2.5,
  isAutomaticVoucherEnabled: false,
  voucherCreditValue: 0,
  isPrepaidCardTopUpEnabled: false,
  prepaidCardCreditValue: 0,
};

/**
 * `GET /api/booking/sharing/general-information`: the sharing settings of the property and their version.
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
 * `POST /api/booking/sharing/save-information` with every setting of the page and the version read in
 * `If-Match`: keeps them (until the page is reloaded), without the days when the minimum is off. 204
 * without a body, the new version in `ETag`; 401 without a valid access token; 400 with one error per
 * wrong field; 409 `resource.version_conflict` when the settings changed after the read.
 */
export const saveSharingInformationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<SharingSaveInformationRequest>;
  const errors = sharingErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const conflict = versionConflict(request, generalInformation);
  if (conflict) {
    return conflict;
  }
  generalInformation = onlyFieldsOf(SharingSaveInformationRequest, {
    ...generalInformation,
    ...body,
    minimumBookingDayQuantity: body.isMinimumBookingDayEnabled
      ? (body.minimumBookingDayQuantity ?? null)
      : null,
  });
  return savedWithVersion(request, generalInformation);
};

/** As the backend checks it: every switch, and the numbers with their limits, more than 0 with their option on. */
function sharingErrors(body: Partial<SharingSaveInformationRequest>): MockFieldError[] {
  const errors: MockFieldError[] = [];
  for (const field of [
    'isSharingEnabled',
    'isMinimumBookingDayEnabled',
    'isOnlySeasonalEnabled',
    'isEnabledOnAllArticles',
    'isScheduledAbsenceSharing',
    'isManualCreditAssignmentEnabled',
    'isAutomaticVoucherEnabled',
    'isPrepaidCardTopUpEnabled',
  ] as const) {
    if (typeof body[field] !== 'boolean') {
      errors.push({ field, code: ApiErrorCode.ValidationInvalid });
    }
  }
  const numbers = [
    ['resoldDayCreditQuantity', body.isManualCreditAssignmentEnabled, MAX_SHARING_QUANTITY, 0],
    ['manualCreditValue', body.isManualCreditAssignmentEnabled, MAX_SHARING_CREDIT_VALUE, 2],
    ['voucherCreditValue', body.isAutomaticVoucherEnabled, MAX_SHARING_CREDIT_VALUE, 2],
    ['prepaidCardCreditValue', body.isPrepaidCardTopUpEnabled, MAX_SHARING_CREDIT_VALUE, 2],
    ...(body.isMinimumBookingDayEnabled
      ? ([['minimumBookingDayQuantity', true, MAX_SHARING_QUANTITY, 0]] as const)
      : []),
  ] as const;
  for (const [field, isOn, highest, decimals] of numbers) {
    const value = body[field];
    if (
      typeof value !== 'number' ||
      value < 0 ||
      (isOn && value === 0) ||
      value > highest ||
      Number(value.toFixed(decimals)) !== value
    ) {
      errors.push({ field, code: ApiErrorCode.ValidationOutOfRange });
    }
  }
  return errors;
}
