import {
  MAX_LENGTH,
  SchemaPathTree,
  ValidationError,
  applyEach,
  applyWhen,
  max,
  metadata,
  min,
  required,
  validate,
} from '@angular/forms/signals';
import { HourlyServiceAddItemRequest } from '../../../../entities/settings/services/hourly-service-add-item-request';
import {
  MAX_CUSTOM_TIME_SLOT_MINUTES,
  ServiceAvailabilityOption,
} from '../../../../entities/settings/services/service-availability-option';
import {
  MAX_SERVICE_BOOKING_QUANTITY,
  ServiceBookingOption,
} from '../../../../entities/settings/services/service-booking-option';
import { MAX_SERVICE_PRICE } from '../../../../entities/settings/services/service-price';
import { ServiceTranslation } from '../../../../entities/settings/services/service-translation';
import {
  MAX_SERVICE_VARIANT_NAME_LENGTH,
  ServiceVariant,
} from '../../../../entities/settings/services/service-variant';
import { minItems, requiredText, timeRange } from '../../../shared/forms/validators';

export interface MissingTranslatedNameError extends ValidationError {
  readonly kind: 'services.translation.name_missing';
}

/** Another language as the backend wants it: where there is a description there is the name too. */
export function otherTranslationRules(translation: SchemaPathTree<ServiceTranslation>): void {
  applyWhen(
    translation,
    ({ valueOf }) => !!valueOf(translation.description)?.trim(),
    (described) => requiredText(described.serviceName),
  );
  validate(translation.description, ({ value, valueOf }): MissingTranslatedNameError | undefined =>
    value()?.trim() && !valueOf(translation.serviceName)?.trim()
      ? { kind: 'services.translation.name_missing' }
      : undefined,
  );
}

/** A variant as the backend wants it: name and price, always. */
export function serviceVariantRules(variant: SchemaPathTree<ServiceVariant>): void {
  requiredText(variant.variantName);
  metadata(variant.variantName, MAX_LENGTH, () => MAX_SERVICE_VARIANT_NAME_LENGTH);
  required(variant.price);
  max(variant.price, MAX_SERVICE_PRICE);
}

/** The hours in their order and the numbers within the backend's limits; every field can be empty. */
export function serviceBookingOptionRules(option: SchemaPathTree<ServiceBookingOption>): void {
  timeRange(option.managementStartTime, option.managementEndTime);
  timeRange(option.onlineStartTime, option.onlineEndTime);
  for (const quantity of [
    option.maxBookingPerHourQuantity,
    option.maxBookingPerDayQuantity,
    option.minItemPerBookingQuantity,
    option.maxItemPerBookingQuantity,
  ]) {
    min(quantity, 1);
    max(quantity, MAX_SERVICE_BOOKING_QUANTITY);
  }
  min(
    option.maxItemPerBookingQuantity,
    ({ valueOf }) => valueOf(option.minItemPerBookingQuantity) ?? undefined,
  );
}

/** An availability as the backend wants it: a day at least, and the price of what can be booked. */
export function serviceAvailabilityRules(option: SchemaPathTree<ServiceAvailabilityOption>): void {
  timeRange(option.startTime, option.endTime);
  minItems(option.daysOfWeek, 1);
  applyEach(option.timeSlotPrices, (timeSlotPrice) => {
    required(timeSlotPrice.price, { when: ({ valueOf }) => valueOf(timeSlotPrice.isEnabled) });
    max(timeSlotPrice.price, MAX_SERVICE_PRICE);
  });
  required(option.customTimeSlotMinuteQuantity, {
    when: ({ valueOf }) =>
      valueOf(option.isCustomTimeSlotEnabled) || valueOf(option.customTimeSlotPrice) !== null,
  });
  min(option.customTimeSlotMinuteQuantity, 1);
  max(option.customTimeSlotMinuteQuantity, MAX_CUSTOM_TIME_SLOT_MINUTES);
  required(option.customTimeSlotPrice, {
    when: ({ valueOf }) => valueOf(option.isCustomTimeSlotEnabled),
  });
  max(option.customTimeSlotPrice, MAX_SERVICE_PRICE);
  min(option.maxBookingPerTimeSlotQuantity, 1);
  max(option.maxBookingPerTimeSlotQuantity, MAX_SERVICE_BOOKING_QUANTITY);
}

/**
 * Price, VAT and availabilities in force of an hourly service as the backend wants them. The VAT is
 * of the service or, without it, of every availability, at least one; a service already saved
 * without VAT (`canLackVat`) can stay so.
 */
export function hourlyServicePriceRules(
  path: SchemaPathTree<HourlyServiceAddItemRequest>,
  canLackVat: () => boolean = () => false,
): void {
  required(path.price);
  max(path.price, MAX_SERVICE_PRICE);
  required(path.vatRate, {
    when: ({ valueOf }) => {
      const options = [
        ...(valueOf(path.isPeriodPricingEnabled)
          ? valueOf(path.periodAvailabilities).flatMap((period) => period.availabilityOptions)
          : valueOf(path.availabilityOptions)),
        ...valueOf(path.bookingOptions).map((option) => option.availabilityOption),
      ];
      return !canLackVat() && !(options.length && options.every(({ vatRate }) => vatRate !== null));
    },
  });
  applyWhen(
    path,
    ({ valueOf }) => !valueOf(path.isPeriodPricingEnabled),
    (always) => applyEach(always.availabilityOptions, serviceAvailabilityRules),
  );
  applyWhen(
    path,
    ({ valueOf }) => valueOf(path.isPeriodPricingEnabled),
    (byPeriod) =>
      applyEach(byPeriod.periodAvailabilities, (periodAvailability) =>
        applyEach(periodAvailability.availabilityOptions, serviceAvailabilityRules),
      ),
  );
}

/** The availabilities in force only, by period or not: the backend keeps one kind of them. */
export function withAvailabilitiesInForce(
  values: HourlyServiceAddItemRequest,
): HourlyServiceAddItemRequest {
  return values.isPeriodPricingEnabled
    ? { ...values, availabilityOptions: [] }
    : { ...values, periodAvailabilities: [] };
}
