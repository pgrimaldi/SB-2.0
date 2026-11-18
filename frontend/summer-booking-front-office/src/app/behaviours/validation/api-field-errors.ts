import { FieldTree, ValidationError } from '@angular/forms/signals';
import { ApiProblem, ApiProblemArgs } from '../../entities/errors/api-problem';

/** The `kind` of every error of the backend on a field: its text is `error.<code>`, not `invalidate.<kind>`. */
export const API_ERROR_KIND = 'api';

/** An error of the backend on one field: its text is the translation `error.<code>`. */
export interface ApiValidationError extends ValidationError.WithFieldTree {
  readonly kind: typeof API_ERROR_KIND;
  readonly code: string;
  readonly args?: ApiProblemArgs;
}

/**
 * An error of the backend on `fieldTree`, e.g. a code that comes without `errors[]` but is about one
 * field: the action of `submit()` returns it and it shows under that field until the field changes.
 */
export function apiFieldError(
  fieldTree: FieldTree<unknown>,
  code: string,
  args?: ApiProblemArgs,
): ApiValidationError {
  return { kind: API_ERROR_KIND, fieldTree, code, args };
}

/**
 * The errors of `problem` that belong to a field of `form` (same name as in the request), for the
 * action of Signal Forms `submit()`: each one is shown under its field until that field changes.
 */
export function apiFieldErrors<TModel extends object>(
  problem: ApiProblem,
  form: FieldTree<TModel>,
): ApiValidationError[] {
  const fields = form as unknown as Readonly<Record<string, FieldTree<unknown>>>;
  const value = form().value();
  return (problem.errors ?? [])
    .filter(({ field }) => Object.hasOwn(value, field) && !isId(field))
    .map(({ field, code, args }) => apiFieldError(fields[field], code, args));
}

/**
 * The same, for a form with a general message too: an error with no field to go under (an id, a
 * field the form has not, or none at all) sends the whole problem there.
 */
export function apiFieldErrorsOrGeneral<TModel extends object>(
  problem: ApiProblem,
  form: FieldTree<TModel>,
  general: (problem: ApiProblem) => void,
): ApiValidationError[] {
  const errors = apiFieldErrors(problem, form);
  if (!errors.length || errors.length < (problem.errors?.length ?? 0)) {
    general(problem);
  }
  return errors;
}

/**
 * An id is never a field the user sees: an error on it goes to the general message, otherwise it
 * would block the form with no visible reason.
 */
function isId(field: string): boolean {
  return /^id[A-Z]/.test(field);
}
