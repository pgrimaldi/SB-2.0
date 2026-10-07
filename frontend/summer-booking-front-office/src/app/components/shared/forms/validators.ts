import {
  PathKind,
  SchemaPath,
  SchemaPathRules,
  ValidationError,
  validate,
} from '@angular/forms/signals';

export interface GreaterThanError extends ValidationError {
  readonly kind: 'greater_than';
  readonly limit: number;
}

export interface RequiredTextError extends ValidationError {
  readonly kind: 'required';
}

/** Unlike Angular's `required`, a text made only of spaces is empty too. */
export function requiredText<TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<string | null, SchemaPathRules.Supported, TPathKind>,
): void {
  validate(path, ({ value }): RequiredTextError | undefined =>
    value()?.trim() ? undefined : { kind: 'required' },
  );
}

/** Unlike Angular's `min`, an empty field is an error too: "greater than 0" also means "required". */
export function greaterThan<TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<number | null, SchemaPathRules.Supported, TPathKind>,
  limit: number,
): void {
  validate(path, ({ value }): GreaterThanError | undefined => {
    const number = value();
    return number !== null && number > limit ? undefined : { kind: 'greater_than', limit };
  });
}
