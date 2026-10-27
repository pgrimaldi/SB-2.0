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

export interface OverlapError extends ValidationError {
  readonly kind: 'overlap';
}

/** First and last day, at midnight UTC as ISO strings. */
export interface DayRange {
  dateFrom: string | null;
  dateTo: string | null;
}

/** At least one day in common; a range without both of its days has none. */
export function rangesOverlap(first: DayRange, second: DayRange): boolean {
  if (!first.dateFrom || !first.dateTo || !second.dateFrom || !second.dateTo) {
    return false;
  }
  return (
    first.dateFrom.slice(0, 10) <= second.dateTo.slice(0, 10) &&
    second.dateFrom.slice(0, 10) <= first.dateTo.slice(0, 10)
  );
}

/** A range of days (the form itself, e.g. Dal / Al) that shares a day with one of `others`. */
export function noOverlap<T extends DayRange, TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<T, SchemaPathRules.Supported, TPathKind>,
  others: () => readonly DayRange[],
): void {
  validate(path, ({ value }): OverlapError | undefined =>
    others().some((other) => rangesOverlap(value(), other)) ? { kind: 'overlap' } : undefined,
  );
}

/** A list of ranges of days where one shares a day with another of the list or with one of `others`. */
export function noOverlaps<T extends DayRange, TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<readonly T[], SchemaPathRules.Supported, TPathKind>,
  others: () => readonly DayRange[],
): void {
  validate(path, ({ value }): OverlapError | undefined => {
    const ranges = value();
    const isClash = ranges.some(
      (range, index) =>
        others().some((other) => rangesOverlap(range, other)) ||
        ranges.some((other, otherIndex) => otherIndex !== index && rangesOverlap(range, other)),
    );
    return isClash ? { kind: 'overlap' } : undefined;
  });
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
