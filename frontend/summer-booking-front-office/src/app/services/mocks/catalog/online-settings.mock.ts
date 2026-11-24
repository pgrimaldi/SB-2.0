import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { OnlineBookingAudience } from '../../../entities/enums/online-booking-audience';
import { OnlineBookingBlock } from '../../../entities/enums/online-booking-block';
import { OnlinePaymentMethod } from '../../../entities/enums/online-payment-method';
import { SupplementType } from '../../../entities/enums/supplement-type';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { MAX_BOOKING_LIMIT_DAYS } from '../../../entities/settings/online-booking-configuration/booking-limit-rule';
import {
  MAX_LEGEND_ITEM_QUANTITY,
  MAX_LEGEND_LABEL_LENGTH,
} from '../../../entities/settings/online-booking-configuration/legend-item';
import { OnlineBookingOptions } from '../../../entities/settings/online-booking-configuration/online-booking-options';
import {
  MAX_ONLINE_BOOKING_HORIZON_DAYS,
  MAX_ONLINE_BOOKING_PERIOD_QUANTITY,
  MAX_ONLINE_BOOKING_QUANTITY,
  OnlineBookingOptionsUpdateRequest,
} from '../../../entities/settings/online-booking-configuration/online-booking-options-update-request';
import { OnlineBookingServicesOptions } from '../../../entities/settings/online-booking-configuration/online-booking-services-options';
import {
  MAX_ONLINE_USEFUL_INFO_LENGTH,
  MAX_SERVICES_SECTION_TITLE_LENGTH,
  MAX_SURCHARGE_AMOUNT,
  MAX_SURCHARGE_PERCENTAGE,
  OnlineBookingServicesOptionsUpdateRequest,
} from '../../../entities/settings/online-booking-configuration/online-booking-services-options-update-request';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { rangesOverlap } from '../../../components/shared/forms/validators';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';
import { savedWithVersion, versionConflict, versionOf } from '../versions/versions.mock';
import { mapCatalogIcon } from './map.mock';
import { SECTORS } from './sectors-data.mock';

const SPOTS: readonly string[] = [
  '7d1e2f30-4a5b-4c6d-8e9f-0a1b2c3d4e01',
  '7d1e2f30-4a5b-4c6d-8e9f-0a1b2c3d4e02',
];

/** As the backend keeps them: days without the time, times with the seconds. */
let bookingOptions: OnlineBookingOptionsUpdateRequest = {
  ...new OnlineBookingOptionsUpdateRequest(),
  isEnabled: true,
  isMapPick: true,
  isLocationRestricted: true,
  allowedSpotIds: [...SPOTS],
  isPeriodRestricted: true,
  allowedPeriods: [{ startsOn: '2027-05-15', endsOn: '2027-09-20' }],
  allowsHalfDay: true,
  morningFrom: '08:00:00',
  morningTo: '13:00:00',
  afternoonFrom: '13:30:00',
  afternoonTo: '19:00:00',
  afternoonConversionTime: '12:30:00',
  allowsSameDayBooking: true,
  surchargeValue: 0,
  onlineBookingBlock: OnlineBookingBlock.ThreeHoursBefore,
  maxSpotsPerBooking: 4,
  bookingHorizonDays: 180,
  scope: OnlineBookingAudience.All,
  stayLimits: [
    {
      id: '3f6a1b20-7c8d-4e9f-a0b1-c2d3e4f5a601',
      sectorId: SECTORS[0].idSector,
      startsOn: '2027-08-01',
      endsOn: '2027-08-31',
      minDays: 3,
      maxDays: 14,
    },
  ],
  paymentMethods: [OnlinePaymentMethod.PayNow, OnlinePaymentMethod.PayAtProperty],
  legend: [
    {
      id: '8b2c4d60-1e2f-4a3b-9c4d-5e6f7a8b9c01',
      label: 'Ombrellone libero',
      iconId: mapCatalogIcon(1).id,
      iconUrl: mapCatalogIcon(1).url,
    },
  ],
  usefulInfo: '<p>Il parcheggio è gratuito per i clienti.</p>',
};

/**
 * `GET /api/catalog/online-settings`: the options of the online bookings, with the limits, the legend
 * and their version. 401 without a valid access token.
 */
export const onlineBookingOptionsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  isAuthorized(request)
    ? of(new HttpResponse({ status: 200, url: request.url, body: bookingAnswer() })).pipe(
        delay(300),
      )
    : unauthorized(request);

/**
 * `PUT /api/catalog/online-settings` with every option and the version read in `If-Match`: keeps them
 * (until the page is reloaded). 200 with the options saved, the new version in `ETag`; 401 without a
 * valid access token; 400 with one error per wrong field; 409 `resource.version_conflict` when the
 * options changed after the read; 404 `catalog.sector_not_found`, `catalog.stay_limit_not_found`,
 * `map.icon_not_found`; 422 `map.spot_not_found`; 409 `catalog.stay_limit_overlap`.
 */
export const updateOnlineBookingOptionsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = request.body as OnlineBookingOptionsUpdateRequest;
  const errors = bookingErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const conflict = versionConflict(request, bookingOptions);
  if (conflict) {
    return conflict;
  }
  const refused = referenceProblem(request, body);
  if (refused) {
    return refused;
  }
  const legend = body.legend.map((item) => {
    const read = bookingOptions.legend.find(({ id }) => item.id !== null && id === item.id);
    const isSameIcon = item.iconId === null ? item.iconUrl !== null : item.iconId === read?.iconId;
    return {
      id: read?.id ?? crypto.randomUUID(),
      label: item.label?.trim() ?? null,
      iconId: item.iconId,
      iconUrl: isSameIcon ? (read?.iconUrl ?? null) : mapCatalogIcon(item.iconId ?? '').url,
    };
  });
  bookingOptions = {
    ...onlyFieldsOf(OnlineBookingOptionsUpdateRequest, body),
    surchargeValue: body.surchargeType === SupplementType.None ? 0 : body.surchargeValue,
    stayLimits: body.stayLimits.map((rule) => ({ ...rule, id: rule.id ?? crypto.randomUUID() })),
    legend,
  };
  return savedWithVersion(request, bookingOptions, bookingAnswer());
};

function bookingAnswer(): OnlineBookingOptions {
  return {
    ...structuredClone(bookingOptions),
    isSuspendedByPlatform: false,
    version: versionOf(bookingOptions),
  };
}

/** The limits of the backend. */
function bookingErrors(body: OnlineBookingOptionsUpdateRequest): MockFieldError[] {
  const errors: MockFieldError[] = [];
  const outOfRange = (field: string, value: number | null, from: number, to: number) => {
    if (value !== null && (value < from || value > to)) {
      errors.push({ field, code: ApiErrorCode.ValidationOutOfRange, args: { from, to } });
    }
  };
  const invalid = (field: string, isInvalid: boolean) => {
    if (isInvalid) {
      errors.push({ field, code: ApiErrorCode.ValidationInvalid });
    }
  };
  invalid('surchargeValue', !isSurcharge(body.surchargeType, body.surchargeValue));
  invalid('scope', !Object.values(OnlineBookingAudience).includes(body.scope));
  invalid(
    'onlineBookingBlock',
    !Object.values(OnlineBookingBlock).includes(body.onlineBookingBlock),
  );
  invalid(
    'paymentMethods',
    !body.paymentMethods.every((method) => Object.values(OnlinePaymentMethod).includes(method)),
  );
  invalid(
    'allowedPeriods',
    body.allowedPeriods.length > MAX_ONLINE_BOOKING_PERIOD_QUANTITY ||
      body.allowedPeriods.some((period, index) =>
        body.allowedPeriods.slice(index + 1).some((other) => rangesOverlap(period, other)),
      ),
  );
  for (const field of ['maxSpotsPerBooking', 'maxBookingsPerDay'] as const) {
    const value = body[field];
    if (value !== null && (value < 1 || value > MAX_ONLINE_BOOKING_QUANTITY)) {
      errors.push({ field, code: ApiErrorCode.ValidationOutOfRange, args: { value: 0 } });
    }
  }
  for (const [from, to] of [
    ['morningFrom', 'morningTo'],
    ['afternoonFrom', 'afternoonTo'],
  ] as const) {
    const start = body[from];
    const end = body[to];
    if (start && end && end <= start) {
      errors.push({ field: to, code: ApiErrorCode.ValidationOutOfRange, args: { value: start } });
    }
  }
  body.allowedPeriods.forEach(({ startsOn, endsOn }, index) => {
    if (startsOn && endsOn && endsOn < startsOn) {
      errors.push({
        field: `allowedPeriods[${index}]`,
        code: ApiErrorCode.ValidationEndBeforeStart,
      });
    }
  });
  outOfRange('bookingHorizonDays', body.bookingHorizonDays, 0, MAX_ONLINE_BOOKING_HORIZON_DAYS);
  body.stayLimits.forEach((rule, index) => {
    const { startsOn, endsOn, minDays, maxDays } = rule;
    const field = `stayLimits[${index}]`;
    invalid(`${field}.id`, isRepeated(body.stayLimits, index));
    for (const name of ['sectorId', 'startsOn', 'endsOn'] as const) {
      if (!rule[name]) {
        errors.push({ field: `${field}.${name}`, code: ApiErrorCode.ValidationRequired });
      }
    }
    if (startsOn && endsOn && endsOn < startsOn) {
      errors.push({ field: `${field}.endsOn`, code: ApiErrorCode.ValidationEndBeforeStart });
    }
    invalid(`${field}.minDays`, minDays === null && maxDays === null);
    outOfRange(`${field}.minDays`, minDays, 1, MAX_BOOKING_LIMIT_DAYS);
    outOfRange(`${field}.maxDays`, maxDays, 1, MAX_BOOKING_LIMIT_DAYS);
    if (minDays !== null && maxDays !== null && maxDays < minDays) {
      errors.push({
        field: `${field}.maxDays`,
        code: ApiErrorCode.ValidationOutOfRange,
        args: { value: minDays },
      });
    }
  });
  invalid('legend', body.legend.length > MAX_LEGEND_ITEM_QUANTITY);
  body.legend.forEach(({ label }, index) => {
    invalid(`legend[${index}].id`, isRepeated(body.legend, index));
    if (!label?.trim()) {
      errors.push({ field: `legend[${index}].label`, code: ApiErrorCode.ValidationRequired });
    } else if (label.length > MAX_LEGEND_LABEL_LENGTH) {
      errors.push({
        field: `legend[${index}].label`,
        code: ApiErrorCode.ValidationMaxLength,
        args: { min: 0, max: MAX_LEGEND_LABEL_LENGTH },
      });
    }
  });
  for (const field of ['goodToKnow', 'usefulInfo'] as const) {
    if ((body[field] ?? '').length > MAX_ONLINE_USEFUL_INFO_LENGTH) {
      errors.push({
        field,
        code: ApiErrorCode.ValidationMaxLength,
        args: { min: 0, max: MAX_ONLINE_USEFUL_INFO_LENGTH },
      });
    }
  }
  return errors;
}

function isRepeated(rows: readonly { id: string | null }[], index: number): boolean {
  const { id } = rows[index];
  return id !== null && rows.findIndex((row) => row.id === id) !== index;
}

/** The elements of the property the save names must exist. */
function referenceProblem(
  request: HttpRequest<unknown>,
  body: OnlineBookingOptionsUpdateRequest,
): Observable<never> | null {
  const { stayLimits, legend, allowedSpotIds } = body;
  if (stayLimits.some(({ sectorId }) => !SECTORS.some(({ idSector }) => idSector === sectorId))) {
    return problem(request, 404, ApiErrorCode.CatalogSectorNotFound);
  }
  if (
    stayLimits.some(({ id }) => id && !bookingOptions.stayLimits.some((rule) => rule.id === id))
  ) {
    return problem(request, 404, ApiErrorCode.CatalogStayLimitNotFound);
  }
  if (
    legend.some(
      ({ id, iconId }) =>
        iconId &&
        !mapCatalogIcon(iconId).id &&
        !bookingOptions.legend.some((item) => item.id === id && item.iconId === iconId),
    )
  ) {
    return problem(request, 404, ApiErrorCode.MapIconNotFound);
  }
  const ids = allowedSpotIds.filter((id) => !SPOTS.includes(id));
  if (ids.length) {
    return problem(request, 422, ApiErrorCode.MapSpotNotFound);
  }
  if (
    stayLimits.some((rule, index) =>
      stayLimits
        .slice(index + 1)
        .some((other) => other.sectorId === rule.sectorId && rangesOverlap(rule, other)),
    )
  ) {
    return problem(request, 409, ApiErrorCode.CatalogStayLimitOverlap);
  }
  return null;
}

function isSurcharge(type: SupplementType, value: number | null): boolean {
  const highest =
    type === SupplementType.Percentage ? MAX_SURCHARGE_PERCENTAGE : MAX_SURCHARGE_AMOUNT;
  return (
    type === SupplementType.None ||
    (Object.values(SupplementType).includes(type) &&
      value !== null &&
      value >= 0 &&
      value <= highest)
  );
}

let servicesOptions: OnlineBookingServicesOptionsUpdateRequest = {
  ...new OnlineBookingServicesOptionsUpdateRequest(),
  surchargeValue: 0,
};

/**
 * `GET /api/catalog/online-settings/services`: the options of the online booking of services and
 * their version; all off and empty until saved. 401 without a valid access token.
 */
export const onlineServicesOptionsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  isAuthorized(request)
    ? of(
        new HttpResponse({ status: 200, url: request.url, body: withVersion(servicesOptions) }),
      ).pipe(delay(300))
    : unauthorized(request);

/**
 * `PUT /api/catalog/online-settings/services` with every option and the version read in `If-Match`:
 * keeps them (until the page is reloaded). 200 with the options saved, the new version in `ETag`;
 * 401 without a valid access token; 400 with one error per wrong field; 409
 * `resource.version_conflict` when the options changed after the read.
 */
export const updateOnlineServicesOptionsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = request.body as OnlineBookingServicesOptionsUpdateRequest;
  const errors = optionsErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const conflict = versionConflict(request, servicesOptions);
  if (conflict) {
    return conflict;
  }
  servicesOptions = {
    ...onlyFieldsOf(OnlineBookingServicesOptionsUpdateRequest, body),
    surchargeValue: body.surchargeType === SupplementType.None ? 0 : body.surchargeValue,
  };
  return savedWithVersion(request, servicesOptions, withVersion(servicesOptions));
};

function withVersion(
  options: OnlineBookingServicesOptionsUpdateRequest,
): OnlineBookingServicesOptions {
  return { ...options, version: versionOf(options) };
}

/** The limits of the backend. */
function optionsErrors(body: OnlineBookingServicesOptionsUpdateRequest): MockFieldError[] {
  const errors: MockFieldError[] = [];
  if (!isSurcharge(body.surchargeType, body.surchargeValue)) {
    errors.push({ field: 'surchargeValue', code: ApiErrorCode.ValidationInvalid });
  }
  if (!body.paymentMethods.every((method) => Object.values(OnlinePaymentMethod).includes(method))) {
    errors.push({ field: 'paymentMethods', code: ApiErrorCode.ValidationInvalid });
  }
  for (const [field, max] of [
    ['sectionTitle', MAX_SERVICES_SECTION_TITLE_LENGTH],
    ['usefulInfo', MAX_ONLINE_USEFUL_INFO_LENGTH],
  ] as const) {
    if ((body[field] ?? '').length > max) {
      errors.push({ field, code: ApiErrorCode.ValidationMaxLength, args: { min: 0, max } });
    }
  }
  return errors;
}
