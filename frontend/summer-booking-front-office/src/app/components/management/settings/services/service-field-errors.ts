import { FieldTree } from '@angular/forms/signals';
import {
  ApiValidationError,
  apiFieldError,
} from '../../../../behaviours/validation/api-field-errors';
import { TableServiceAvailabilityMode } from '../../../../entities/enums/table-service-availability-mode';
import { TableServiceBookingPayment } from '../../../../entities/enums/table-service-booking-payment';
import { ApiErrorCode } from '../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import { AutomaticPriceAddItemRequest } from '../../../../entities/settings/services/automatic-price-add-item-request';
import { HourlyServiceDraft } from '../../../../entities/settings/services/hourly-service-draft';
import { TableServiceAddItemRequest } from '../../../../entities/settings/services/table-service-add-item-request';

type ServiceErrorField = 'icon' | 'vatRate';

/** The codes of the backend that come without `errors[]` and are about one field of the form. */
const FIELD_OF_CODE: Readonly<Partial<Record<string, ServiceErrorField>>> = {
  [ApiErrorCode.CatalogVatRateNotAllowed]: 'vatRate',
  [ApiErrorCode.ServiceIconNotFound]: 'icon',
};

/** The error under its field, for the codes that are about one; null for the others. */
export function serviceFieldError(
  problem: ApiProblem,
  form: Readonly<Partial<Record<ServiceErrorField, FieldTree<unknown>>>>,
): ApiValidationError | null {
  const field = FIELD_OF_CODE[problem.code];
  const fieldTree = field && form[field];
  return fieldTree ? apiFieldError(fieldTree, problem.code, problem.args) : null;
}

/** The fields of an automatic price with no place for their error. */
export const HIDDEN_AUTOMATIC_PRICE_FIELDS: readonly (keyof AutomaticPriceAddItemRequest)[] = [
  'isPricePerServiceEnabled',
];

/** The fields of an hourly service with no place for their error, as it is filled now. */
export function hiddenHourlyServiceFields({
  isPeriodPricingEnabled,
}: HourlyServiceDraft): readonly (keyof HourlyServiceDraft)[] {
  return isPeriodPricingEnabled ? ['availabilityOptions'] : [];
}

/** The fields of a table service with no place for their error, as it is filled now. */
export function hiddenTableServiceFields({
  availabilityMode,
  bookingPayment,
}: TableServiceAddItemRequest): readonly (keyof TableServiceAddItemRequest)[] {
  return [
    'availabilityMode',
    'bookingPayment',
    'depositAmount',
    'minSeatPerBookingQuantity',
    'maxSeatPerBookingQuantity',
    availabilityMode === TableServiceAvailabilityMode.Tables ? 'totalSeatQuantity' : 'tableGroups',
    ...(bookingPayment === TableServiceBookingPayment.Deposit
      ? []
      : (['depositMode', 'depositPercentage'] as const)),
  ];
}

/** The fields of the first level that the error is about. */
export function serviceErrorFields(problem: ApiProblem): string[] {
  const field = FIELD_OF_CODE[problem.code];
  return field ? [field] : (problem.errors ?? []).map((error) => error.field.split(/[.[]/, 1)[0]);
}
