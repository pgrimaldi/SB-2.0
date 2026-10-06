import { FieldTree, ValidationError } from '@angular/forms/signals';
import { ApiProblem, ApiProblemArgs } from '../../entities/errors/api-problem';

/** An error of the backend on one field: its text is the translation `error.<code>`. */
export interface ApiValidationError extends ValidationError.WithFieldTree {
  readonly kind: 'api';
  readonly code: string;
  readonly args?: ApiProblemArgs;
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
    .filter(({ field }) => Object.hasOwn(value, field))
    .map(({ field, code, args }) => ({ kind: 'api', fieldTree: fields[field], code, args }));
}
