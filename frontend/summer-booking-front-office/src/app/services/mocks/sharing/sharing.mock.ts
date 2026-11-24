import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { SharingGeneralInformation } from '../../../entities/settings/sharing/sharing-general-information';
import {
  MAX_SHARING_BOOKING_DAYS,
  MAX_SHARING_CREDIT_AMOUNT,
  MAX_SHARING_CREDIT_UNITS,
  SharingSaveInformationRequest,
} from '../../../entities/settings/sharing/sharing-save-information-request';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';
import { savedWithVersion, versionConflict, versionOf } from '../versions/versions.mock';

let settings: SharingSaveInformationRequest = {
  isEnabled: true,
  minBookingDays: 3,
  seasonalOnly: false,
  allArticles: true,
  absencesAsSharing: true,
  manualCreditEnabled: false,
  manualCreditUnitsPerDay: 1,
  manualCreditAmount: 2.5,
  voucherCreditEnabled: false,
  voucherCreditAmount: 0,
  cardTopUpEnabled: false,
  cardTopUpAmount: 0,
};

/**
 * `GET /api/booking/sharing/settings`: the sharing settings of the property and their version. 401
 * without a valid access token.
 */
export const sharingSettingsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  isAuthorized(request)
    ? of(new HttpResponse({ status: 200, url: request.url, body: withVersion(settings) })).pipe(
        delay(150),
      )
    : unauthorized(request);

/**
 * `PUT /api/booking/sharing/settings` with every setting of the page and the version read in
 * `If-Match`: keeps them (until the page is reloaded). 200 with the settings saved, the new version in
 * `ETag`; 401 without a valid access token; 400 with one error per wrong field; 409
 * `resource.version_conflict` when the settings changed after the read.
 */
export const saveSharingSettingsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = request.body as SharingSaveInformationRequest;
  const errors = settingsErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const conflict = versionConflict(request, settings);
  if (conflict) {
    return conflict;
  }
  settings = onlyFieldsOf(SharingSaveInformationRequest, body);
  return savedWithVersion(request, settings, withVersion(settings));
};

function withVersion(saved: SharingSaveInformationRequest): SharingGeneralInformation {
  return { ...saved, version: versionOf(saved) };
}

/** The limits of the backend, and more than 0 for the numbers of an option that is on. */
function settingsErrors(body: SharingSaveInformationRequest): MockFieldError[] {
  const errors: MockFieldError[] = [];
  const check = (
    field: 'minBookingDays' | 'manualCreditUnitsPerDay',
    lowest: number,
    highest: number,
  ) => {
    const value = body[field] ?? 0;
    if (value < lowest || value > highest) {
      errors.push({
        field,
        code: ApiErrorCode.ValidationOutOfRange,
        args: { from: lowest, to: highest },
      });
    }
  };
  if (body.minBookingDays !== null) {
    check('minBookingDays', 1, MAX_SHARING_BOOKING_DAYS);
  }
  check('manualCreditUnitsPerDay', body.manualCreditEnabled ? 1 : 0, MAX_SHARING_CREDIT_UNITS);
  for (const [field, isOn] of [
    ['manualCreditAmount', body.manualCreditEnabled],
    ['voucherCreditAmount', body.voucherCreditEnabled],
    ['cardTopUpAmount', body.cardTopUpEnabled],
  ] as const) {
    const value = body[field] ?? 0;
    if (value < 0 || value > MAX_SHARING_CREDIT_AMOUNT || (isOn && value === 0)) {
      errors.push({
        field,
        code: ApiErrorCode.ValidationOutOfRange,
        args: { from: 0, to: MAX_SHARING_CREDIT_AMOUNT },
      });
    } else if (Number(value.toFixed(2)) !== value) {
      errors.push({ field, code: ApiErrorCode.ValidationInvalid });
    }
  }
  return errors;
}
