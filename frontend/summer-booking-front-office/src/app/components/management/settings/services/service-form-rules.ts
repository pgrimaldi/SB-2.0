import {
  MAX_LENGTH,
  SchemaPathTree,
  ValidationError,
  applyWhen,
  max,
  metadata,
  min,
  required,
  validate,
} from '@angular/forms/signals';
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
import { requiredText, timeRange } from '../../../shared/forms/validators';

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
