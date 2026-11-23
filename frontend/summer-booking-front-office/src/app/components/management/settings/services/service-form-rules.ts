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
import { AutomaticPriceAddItemRequest } from '../../../../entities/settings/services/automatic-price-add-item-request';
import { HourlyServiceAddItemRequest } from '../../../../entities/settings/services/hourly-service-add-item-request';
import { HourlyServiceDraft } from '../../../../entities/settings/services/hourly-service-draft';
import {
  MAX_CUSTOM_TIME_SLOT_MINUTES,
  ServiceAvailabilityOption,
} from '../../../../entities/settings/services/service-availability-option';
import { ServiceAvailabilityOptionDraft } from '../../../../entities/settings/services/service-availability-option-draft';
import {
  MAX_SERVICE_BOOKING_QUANTITY,
  ServiceBookingOption,
} from '../../../../entities/settings/services/service-booking-option';
import { MAX_SERVICE_PRICE } from '../../../../entities/settings/services/service-price';
import { TableServiceAvailabilityMode } from '../../../../entities/enums/table-service-availability-mode';
import { TableServiceBookingPayment } from '../../../../entities/enums/table-service-booking-payment';
import { TableServiceAddItemRequest } from '../../../../entities/settings/services/table-service-add-item-request';
import {
  MAX_SEAT_PER_TABLE_QUANTITY,
  MAX_TABLE_QUANTITY,
  MAX_TOTAL_SEAT_QUANTITY,
} from '../../../../entities/settings/services/table-service-table-group';
import { ServiceTranslation } from '../../../../entities/settings/services/service-translation';
import {
  MAX_SERVICE_VARIANT_NAME_LENGTH,
  ServiceVariant,
} from '../../../../entities/settings/services/service-variant';
import { onlyFieldsOf } from '../../../../services/api/only-fields-of';
import { comparableJson } from '../../../shared/forms/text-comparison';
import {
  MinItemsError,
  greaterThan,
  minItems,
  requiredText,
  timeRange,
} from '../../../shared/forms/validators';

export interface MissingTranslatedNameError extends ValidationError {
  readonly kind: 'services.translation.name_missing';
}

export interface SeatsTakenError extends ValidationError {
  readonly kind: 'services.tables.seats_taken';
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
export function serviceAvailabilityRules(
  option: SchemaPathTree<ServiceAvailabilityOptionDraft>,
): void {
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
 * of the service or, without it, of every availability sent, at least one; a service already saved
 * without VAT (`canLackVat`) can stay so.
 */
export function hourlyServicePriceRules(
  path: SchemaPathTree<HourlyServiceDraft>,
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
      ].filter(isAvailabilitySent);
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

/**
 * An automatic price as the backend wants it: the price always, a day at least and both hours of the
 * time slots in every period, a service at least unless it applies to all of them.
 */
export function automaticPriceRules(path: SchemaPathTree<AutomaticPriceAddItemRequest>): void {
  required(path.price);
  max(path.price, MAX_SERVICE_PRICE);
  applyEach(path.validityPeriods, (period) => {
    minItems(period.daysOfWeek, 1);
    applyEach(period.timeSlots, (timeSlot) => {
      required(timeSlot.startTime);
      required(timeSlot.endTime);
      timeRange(timeSlot.startTime, timeSlot.endTime);
    });
  });
  validate(path.isAllServicesEnabled, ({ value, valueOf }): MinItemsError | undefined =>
    value() ||
    [path.idBarServices, path.idStandardServices, path.idHourlyServices].some(
      (ids) => valueOf(ids).length > 0,
    )
      ? undefined
      : { kind: 'min_items', count: 1 },
  );
}

/** The services chosen only when the automatic price does not apply to all of them. */
export function withServicesInForce(
  values: AutomaticPriceAddItemRequest,
): AutomaticPriceAddItemRequest {
  return values.isAllServicesEnabled
    ? { ...values, idBarServices: [], idStandardServices: [], idHourlyServices: [] }
    : values;
}

const EMPTY_AVAILABILITY = comparableJson(new ServiceAvailabilityOption());

/** One left empty is only what the panel shows where there are none, unless it was saved so. */
function isAvailabilitySent(option: ServiceAvailabilityOptionDraft): boolean {
  return (
    option.isSaved ||
    comparableJson(onlyFieldsOf(ServiceAvailabilityOption, option)) !== EMPTY_AVAILABILITY
  );
}

/** The availabilities to send: a period without any is left out, a booking option has `null`. */
export function withoutEmptyAvailabilities(
  values: HourlyServiceDraft,
): HourlyServiceAddItemRequest {
  return {
    ...values,
    availabilityOptions: values.availabilityOptions.filter(isAvailabilitySent),
    periodAvailabilities: values.periodAvailabilities
      .map((period) => ({
        ...period,
        availabilityOptions: period.availabilityOptions.filter(isAvailabilitySent),
      }))
      .filter(({ availabilityOptions }) => availabilityOptions.length > 0),
    bookingOptions: values.bookingOptions.map((option) => ({
      ...option,
      availabilityOption: isAvailabilitySent(option.availabilityOption)
        ? option.availabilityOption
        : null,
    })),
  };
}

/** The availabilities to send of the kind in force only: the backend keeps one kind of them. */
export function withAvailabilitiesInForce(values: HourlyServiceDraft): HourlyServiceAddItemRequest {
  const sent = withoutEmptyAvailabilities(values);
  return sent.isPeriodPricingEnabled
    ? { ...sent, availabilityOptions: [] }
    : { ...sent, periodAvailabilities: [] };
}

const MAX_PERCENTAGE = 100;

/**
 * A table service as the backend wants it: the days of every availability with their hours, the
 * tables or the seats, the deposit.
 */
export function tableServiceRules(path: SchemaPathTree<TableServiceAddItemRequest>): void {
  applyEach(path.availabilities, (availability) => {
    required(availability.dateFrom);
    required(availability.dateTo);
    required(availability.startTime);
    required(availability.endTime);
    timeRange(availability.startTime, availability.endTime);
    validate(availability.days, ({ value }): MinItemsError | undefined =>
      value().some(({ isEnabled }) => isEnabled) ? undefined : { kind: 'min_items', count: 1 },
    );
    applyEach(availability.days, (day) => {
      required(day.startTime, { when: ({ valueOf }) => valueOf(day.isEnabled) });
      required(day.endTime, { when: ({ valueOf }) => valueOf(day.isEnabled) });
      timeRange(day.startTime, day.endTime);
    });
  });
  applyWhen(
    path,
    ({ valueOf }) => valueOf(path.availabilityMode) === TableServiceAvailabilityMode.Tables,
    (tables) => {
      minItems(tables.tableGroups, 1);
      applyEach(tables.tableGroups, (tableGroup) => {
        required(tableGroup.tableQuantity);
        min(tableGroup.tableQuantity, 1);
        max(tableGroup.tableQuantity, MAX_TABLE_QUANTITY);
        required(tableGroup.seatPerTableQuantity);
        min(tableGroup.seatPerTableQuantity, 1);
        max(tableGroup.seatPerTableQuantity, MAX_SEAT_PER_TABLE_QUANTITY);
        validate(
          tableGroup.seatPerTableQuantity,
          ({ value, valueOf }): SeatsTakenError | undefined =>
            value() !== null &&
            valueOf(tables.tableGroups).filter(
              ({ seatPerTableQuantity }) => seatPerTableQuantity === value(),
            ).length > 1
              ? { kind: 'services.tables.seats_taken' }
              : undefined,
        );
      });
    },
  );
  applyWhen(
    path,
    ({ valueOf }) => valueOf(path.availabilityMode) === TableServiceAvailabilityMode.Seats,
    (seats) => {
      required(seats.totalSeatQuantity);
      min(seats.totalSeatQuantity, 1);
      max(seats.totalSeatQuantity, MAX_TOTAL_SEAT_QUANTITY);
    },
  );
  min(path.maxBookingPerTimeSlotQuantity, 1);
  max(path.maxBookingPerTimeSlotQuantity, MAX_SERVICE_BOOKING_QUANTITY);
  applyWhen(
    path,
    ({ valueOf }) => valueOf(path.bookingPayment) === TableServiceBookingPayment.Deposit,
    (deposit) => {
      required(deposit.depositPercentage, {
        when: ({ valueOf }) => valueOf(deposit.depositAmount) === null,
      });
      max(deposit.depositPercentage, MAX_PERCENTAGE);
      applyWhen(
        deposit,
        ({ valueOf }) => valueOf(deposit.depositPercentage) !== null,
        (written) => greaterThan(written.depositPercentage, 0),
      );
    },
  );
}
