import { FieldTree } from '@angular/forms/signals';
import {
  ApiValidationError,
  apiFieldError,
} from '../../../../behaviours/validation/api-field-errors';
import { ApiErrorCode } from '../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../entities/errors/api-problem';

type ServiceErrorField = 'icon' | 'vatRate';

/** The codes of the backend that come without `errors[]` and are about one field of the form. */
const FIELD_OF_CODE: Readonly<Partial<Record<string, ServiceErrorField>>> = {
  [ApiErrorCode.CatalogVatRateNotAllowed]: 'vatRate',
  [ApiErrorCode.ServiceIconNotFound]: 'icon',
};

/** The error under its field, for the codes that are about one; null for the others. */
export function serviceFieldError(
  problem: ApiProblem,
  form: Readonly<Record<ServiceErrorField, FieldTree<unknown>>>,
): ApiValidationError | null {
  const field = FIELD_OF_CODE[problem.code];
  return field ? apiFieldError(form[field], problem.code, problem.args) : null;
}

/** The fields of the first level that the error is about. */
export function serviceErrorFields(problem: ApiProblem): string[] {
  const field = FIELD_OF_CODE[problem.code];
  return field ? [field] : (problem.errors ?? []).map((error) => error.field.split(/[.[]/, 1)[0]);
}
