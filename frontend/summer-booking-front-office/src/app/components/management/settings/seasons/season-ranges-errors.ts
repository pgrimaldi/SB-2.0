import { FieldTree, ValidationError } from '@angular/forms/signals';
import {
  ApiValidationError,
  apiFieldError,
} from '../../../../behaviours/validation/api-field-errors';
import { ApiErrorCode } from '../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import {
  MAX_SEASON_PERIOD_RANGES,
  SeasonPeriodItem,
} from '../../../../entities/settings/seasons/season-period-item';

const RANGES_FIELD = 'ranges' satisfies keyof SeasonPeriodItem;

export interface RangesInvalidError extends ValidationError.WithFieldTree {
  readonly kind: 'seasons.ranges.invalid';
  readonly max: number;
}

/**
 * The errors of the backend on the intervals of a period (`ranges`, `ranges[i]…`), all on `fieldTree`
 * with their own text: the calendar in Imposta periodo, the whole form in the popups without a field
 * for them (the text shows above the buttons). Too many intervals or days in common have one text.
 */
export function rangesErrors(
  problem: ApiProblem,
  fieldTree: FieldTree<unknown>,
): (RangesInvalidError | ApiValidationError)[] {
  return (problem.errors ?? [])
    .filter(({ field }) => isRangesField(field))
    .map(({ field, code, args }) =>
      field === RANGES_FIELD && code === ApiErrorCode.ValidationInvalid
        ? { kind: 'seasons.ranges.invalid', max: MAX_SEASON_PERIOD_RANGES, fieldTree }
        : apiFieldError(fieldTree, code, args),
    );
}

/** The same problem without its errors on the intervals. */
export function withoutRangesErrors(problem: ApiProblem): ApiProblem {
  return {
    ...problem,
    errors: (problem.errors ?? []).filter(({ field }) => !isRangesField(field)),
  };
}

function isRangesField(field: string): boolean {
  return field === RANGES_FIELD || field.startsWith(`${RANGES_FIELD}[`);
}
