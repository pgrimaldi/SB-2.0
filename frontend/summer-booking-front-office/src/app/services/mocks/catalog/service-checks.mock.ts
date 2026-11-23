import { HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Weekday } from '../../../entities/enums/weekday';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import {
  MAX_SERVICE_BOOKING_QUANTITY,
  ServiceBookingOption,
} from '../../../entities/settings/services/service-booking-option';
import {
  MAX_SERVICE_DESCRIPTION_LENGTH,
  MAX_SERVICE_NAME_LENGTH,
  ServiceTranslation,
} from '../../../entities/settings/services/service-translation';
import { MockFieldError, problem } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';
import { PERIODS } from '../seasons/periods-data.mock';
import { SECTORS } from './sectors-data.mock';

const MAX_SECTOR_QUANTITY = 500;
const MAX_BOOKING_OPTION_QUANTITY = 20;

/**
 * Name and description in the language of the request (the user's), within their limits; in the
 * others the name where there is a description, one error for all of them.
 */
export function translationErrors(
  translations: readonly ServiceTranslation[] | undefined,
  language: string,
): MockFieldError[] {
  const all = Array.isArray(translations) ? translations : [];
  const index = all.findIndex((translation) => translation.language === language);
  const main = all[index];
  const at = `translations[${Math.max(index, 0)}]`;
  const errors = [
    ...mainTextErrors(`${at}.serviceName`, main?.serviceName, MAX_SERVICE_NAME_LENGTH),
    ...mainTextErrors(`${at}.description`, main?.description, MAX_SERVICE_DESCRIPTION_LENGTH),
  ];
  const isOtherWrong = all.some(
    (translation) =>
      translation !== main &&
      ((translation.description?.trim() && !translation.serviceName?.trim()) ||
        (translation.serviceName ?? '').trim().length > MAX_SERVICE_NAME_LENGTH ||
        (translation.description ?? '').trim().length > MAX_SERVICE_DESCRIPTION_LENGTH),
  );
  return isOtherWrong
    ? [...errors, { field: 'translations', code: ApiErrorCode.ValidationInvalid }]
    : errors;
}

/** One of the icons, or the one the element has already. */
export function iconErrors(
  icon: string | null | undefined,
  icons: readonly string[],
  savedIcon: string | null = null,
): MockFieldError[] {
  return icon == null || icon === savedIcon || icons.includes(icon)
    ? []
    : [{ field: 'icon', code: ApiErrorCode.ValidationInvalid }];
}

export function sectorErrors(idSectors: readonly string[] | undefined): MockFieldError[] {
  return (idSectors?.length ?? 0) > MAX_SECTOR_QUANTITY
    ? [{ field: 'idSectors', code: ApiErrorCode.ValidationInvalid }]
    : [];
}

/** 404 `catalog.sector_not_found` for an id that is not a sector of the property. */
export function missingSector(
  request: HttpRequest<unknown>,
  idSectors: readonly string[] | undefined,
): Observable<never> | null {
  const isMissing = (idSectors ?? []).some(
    (id) =>
      !SECTORS.some(
        ({ propertyId, idSector }) => propertyId === DEMO_PROPERTY.id && idSector === id,
      ),
  );
  return isMissing ? problem(request, 404, ApiErrorCode.CatalogSectorNotFound) : null;
}

/** 422 `pricing.period_not_found` for a period neither of the current season nor already saved. */
export function missingPeriod(
  request: HttpRequest<unknown>,
  idPeriods: readonly (number | null)[],
  savedIdPeriods: readonly (number | null)[] = [],
): Observable<never> | null {
  const isMissing = idPeriods.some(
    (idPeriod) =>
      !savedIdPeriods.includes(idPeriod) &&
      !PERIODS.some(
        ({ propertyId, isNextSeason, key }) =>
          propertyId === DEMO_PROPERTY.id && !isNextSeason && key === idPeriod,
      ),
  );
  return isMissing ? problem(request, 422, ApiErrorCode.PricingPeriodNotFound) : null;
}

/** Days and hours in their order, quantities in their limits and the weekdays of their enum. */
export function bookingOptionErrors(
  bookingOptions: readonly ServiceBookingOption[] | undefined,
): MockFieldError[] {
  const options = bookingOptions ?? [];
  const invalid = (field: string): MockFieldError => ({
    field,
    code: ApiErrorCode.ValidationInvalid,
  });
  return [
    ...(options.length > MAX_BOOKING_OPTION_QUANTITY ? [invalid('bookingOptions')] : []),
    ...options.flatMap((option, index): MockFieldError[] => {
      const at = `bookingOptions[${index}]`;
      const { minItemPerBookingQuantity: minimum, maxItemPerBookingQuantity: maximum } = option;
      const quantities = {
        maxBookingPerHourQuantity: option.maxBookingPerHourQuantity,
        maxBookingPerDayQuantity: option.maxBookingPerDayQuantity,
        maxPerSlot: option.maxPerSlot,
        intervalMinutes: option.intervalMinutes,
        minItemPerBookingQuantity: minimum,
        maxItemPerBookingQuantity: maximum,
      };
      return [
        ...(option.dateFrom && option.dateTo && option.dateTo < option.dateFrom
          ? [{ field: `${at}.dateTo`, code: ApiErrorCode.ValidationEndBeforeStart }]
          : []),
        ...(isNotLater(option.managementEndTime, option.managementStartTime)
          ? [invalid(`${at}.managementEndTime`)]
          : []),
        ...(isNotLater(option.onlineEndTime, option.onlineStartTime)
          ? [invalid(`${at}.onlineEndTime`)]
          : []),
        ...(option.weekdays.every((weekday) => Object.values(Weekday).includes(weekday))
          ? []
          : [invalid(`${at}.weekdays`)]),
        ...Object.entries(quantities)
          .filter(
            ([field, quantity]) =>
              (quantity !== null &&
                (!Number.isInteger(quantity) ||
                  quantity < 1 ||
                  quantity > MAX_SERVICE_BOOKING_QUANTITY)) ||
              (field === 'maxItemPerBookingQuantity' &&
                minimum !== null &&
                maximum !== null &&
                maximum < minimum),
          )
          .map(([field]) => invalid(`${at}.${field}`)),
      ];
    }),
  ];
}

export function isNotLater(time: string | null, other: string | null): boolean {
  return !!time && !!other && time <= other;
}

function mainTextErrors(
  field: string,
  text: string | null | undefined,
  max: number,
): MockFieldError[] {
  if (!text?.trim()) {
    return [{ field, code: ApiErrorCode.ValidationRequired }];
  }
  return text.trim().length > max
    ? [{ field, code: ApiErrorCode.ValidationMaxLength, args: { max } }]
    : [];
}
