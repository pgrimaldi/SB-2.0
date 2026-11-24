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
 * `hiddenFields` are the fields with no place for their error.
 */
export function apiFieldErrors<TModel extends object>(
  problem: ApiProblem,
  form: FieldTree<TModel>,
  hiddenFields: readonly string[] = [],
): ApiValidationError[] {
  const fields = form as unknown as Readonly<Record<string, FieldTree<unknown>>>;
  return (problem.errors ?? [])
    .filter(({ field }) => hasPlace(field, form, hiddenFields))
    .map(({ field, code, args }) => apiFieldError(fields[field], code, args));
}

/**
 * The same, for a form with a general message too: an error with no field to go under (an id, a
 * field the form has not or does not show, or none at all) sends the problem there, with the code of
 * the first of these errors when it is about a field the user fills (e.g. inside a list).
 */
export function apiFieldErrorsOrGeneral<TModel extends object>(
  problem: ApiProblem,
  form: FieldTree<TModel>,
  general: (problem: ApiProblem) => void,
  hiddenFields: readonly string[] = [],
): ApiValidationError[] {
  const errors = apiFieldErrors(problem, form, hiddenFields);
  if (!errors.length || errors.length < (problem.errors?.length ?? 0)) {
    const first = problem.errors?.find(
      ({ field }) => !hasPlace(field, form, hiddenFields) && !field.split('.').some(isId),
    );
    general(first ? { ...problem, code: first.code, args: first.args } : problem);
  }
  return errors;
}

/**
 * Takes away the errors of the backend under the fields of `form`, e.g. with Annulla: Angular drops
 * them only when the value of their field changes, and `reset()` keeps them.
 */
export function clearApiFieldErrors<TModel extends object>(form: FieldTree<TModel>): void {
  const fields = form as unknown as Readonly<Record<string, FieldTree<unknown>>>;
  for (const name of Object.keys(form().value())) {
    const field = fields[name]();
    if (field.errors().some(({ kind }) => kind === API_ERROR_KIND)) {
      const value = field.value();
      field.value.set(Array.isArray(value) ? [...value] : value === null ? '' : null);
      field.errors();
      field.value.set(value);
    }
  }
}

function hasPlace<TModel extends object>(
  field: string,
  form: FieldTree<TModel>,
  hiddenFields: readonly string[],
): boolean {
  return Object.hasOwn(form().value(), field) && !isId(field) && !hiddenFields.includes(field);
}

/**
 * An id is never a field the user sees: an error on it goes to the general message, otherwise it
 * would block the form with no visible reason.
 */
function isId(field: string): boolean {
  return /^id[A-Z]/.test(field);
}
