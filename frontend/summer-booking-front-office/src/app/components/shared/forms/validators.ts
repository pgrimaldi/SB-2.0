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

export interface MinItemsError extends ValidationError {
  readonly kind: 'min_items';
  readonly count: number;
}

export interface MinLengthError extends ValidationError {
  readonly kind: 'min_length';
  readonly min: number;
}

export interface MissingDigitError extends ValidationError {
  readonly kind: 'missing_digit';
}

export interface MissingUppercaseError extends ValidationError {
  readonly kind: 'missing_uppercase';
}

export interface MissingLowercaseError extends ValidationError {
  readonly kind: 'missing_lowercase';
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

/** A list with fewer than `count` items, e.g. a voucher without validity periods. */
export function minItems<TItem, TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<readonly TItem[], SchemaPathRules.Supported, TPathKind>,
  count: number,
): void {
  validate(path, ({ value }): MinItemsError | undefined =>
    value().length >= count ? undefined : { kind: 'min_items', count },
  );
}

/**
 * A password with at least `minLength` characters, an uppercase letter, a lowercase letter and a
 * digit, in this order of errors. An empty field is left to `requiredText`.
 */
export function passwordRules<TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<string | null, SchemaPathRules.Supported, TPathKind>,
  minLength: number,
): void {
  validate(path, ({ value }): MinLengthError | undefined => {
    const password = value();
    return !password || password.length >= minLength
      ? undefined
      : { kind: 'min_length', min: minLength };
  });
  validate(path, ({ value }): MissingUppercaseError | undefined =>
    !value() || /\p{Lu}/u.test(value() ?? '') ? undefined : { kind: 'missing_uppercase' },
  );
  validate(path, ({ value }): MissingLowercaseError | undefined =>
    !value() || /\p{Ll}/u.test(value() ?? '') ? undefined : { kind: 'missing_lowercase' },
  );
  validate(path, ({ value }): MissingDigitError | undefined =>
    !value() || /\p{Nd}/u.test(value() ?? '') ? undefined : { kind: 'missing_digit' },
  );
}
