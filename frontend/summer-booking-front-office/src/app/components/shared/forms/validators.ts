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

export const OVERLAP_KIND = 'overlap';

export interface OverlapError extends ValidationError {
  readonly kind: typeof OVERLAP_KIND;
}

export interface WebLinkError extends ValidationError {
  readonly kind: 'web_link';
}

export interface EmailError extends ValidationError {
  readonly kind: 'email';
}

export interface MaxLengthError extends ValidationError {
  readonly kind: 'max_length';
  readonly max: number;
}

export interface MaxHtmlLengthError extends ValidationError {
  readonly kind: 'max_html_length';
  readonly max: number;
}

export interface PhoneDigitsError extends ValidationError {
  readonly kind: 'phone_digits';
}

export interface PhoneDigitsWithoutPrefixError extends ValidationError {
  readonly kind: 'phone_digits_without_prefix';
}

export interface PhoneMaxLengthError extends ValidationError {
  readonly kind: 'phone_max_length';
  readonly max: number;
}

export interface TimeBeforeEndError extends ValidationError {
  readonly kind: 'time_before_end';
}

export interface TimeAfterStartError extends ValidationError {
  readonly kind: 'time_after_start';
}

/** First and last day, at midnight UTC as ISO strings. */
export interface DayRange {
  startsOn: string | null;
  endsOn: string | null;
}

/** At least one day in common; a range without both of its days has none. */
export function rangesOverlap(first: DayRange, second: DayRange): boolean {
  if (!first.startsOn || !first.endsOn || !second.startsOn || !second.endsOn) {
    return false;
  }
  return (
    first.startsOn.slice(0, 10) <= second.endsOn.slice(0, 10) &&
    second.startsOn.slice(0, 10) <= first.endsOn.slice(0, 10)
  );
}

/** The same text as one of `others`, not minding capitals and the spaces at the ends (e.g. a name already used). */
export function isTextTaken(text: string | null, others: readonly (string | null)[]): boolean {
  const wanted = text?.trim().toLowerCase();
  return !!wanted && others.some((other) => other?.trim().toLowerCase() === wanted);
}

/** A range of days (the form itself, e.g. Dal / Al) that shares a day with one of `others`. */
export function noOverlap<T extends DayRange, TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<T, SchemaPathRules.Supported, TPathKind>,
  others: () => readonly DayRange[],
): void {
  validate(path, ({ value }): OverlapError | undefined =>
    others().some((other) => rangesOverlap(value(), other)) ? { kind: OVERLAP_KIND } : undefined,
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
    return isClash ? { kind: OVERLAP_KIND } : undefined;
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

/** A text that is not a full http or https address; an empty field is left to `requiredText`. */
export function webLink<TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<string | null, SchemaPathRules.Supported, TPathKind>,
): void {
  validate(path, ({ value }): WebLinkError | undefined => {
    const text = value()?.trim();
    return !text || isWebLink(text) ? undefined : { kind: 'web_link' };
  });
}

/** Like the backend: one `@`, neither first nor last. An empty field is left to `requiredText`. */
export function emailAddress<TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<string | null, SchemaPathRules.Supported, TPathKind>,
): void {
  validate(path, ({ value }): EmailError | undefined => {
    const email = value()?.trim();
    return !email || isEmailAddress(email) ? undefined : { kind: 'email' };
  });
}

/** The backend's check of an email: one `@`, neither first nor last. */
export function isEmailAddress(text: string): boolean {
  const at = text.indexOf('@');
  return at > 0 && at < text.length - 1 && at === text.lastIndexOf('@');
}

/** Rich text (HTML) whose text, as the user sees it, is longer than `max` characters. */
export function maxRichTextLength<TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<string | null, SchemaPathRules.Supported, TPathKind>,
  max: number,
): void {
  validate(path, ({ value }): MaxLengthError | undefined =>
    visibleTextLength(value()) > max ? { kind: 'max_length', max } : undefined,
  );
}

/** Rich text (HTML) longer than `max` characters with its formatting, as the backend counts them. */
export function maxHtmlLength<TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<string | null, SchemaPathRules.Supported, TPathKind>,
  max: number,
): void {
  validate(path, ({ value }): MaxHtmlLengthError | undefined =>
    (value()?.length ?? 0) > max ? { kind: 'max_html_length', max } : undefined,
  );
}

/**
 * A phone number with something other than digits and spaces (the prefix has its own field), then
 * one longer than `maxLength` characters, spaces included.
 */
export function phoneNumber<TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<string | null, SchemaPathRules.Supported, TPathKind>,
  maxLength: number,
): void {
  validate(path, ({ value }) => phoneError(value(), maxLength, { kind: 'phone_digits' }));
}

/** The same rules, for a phone number with no prefix field beside it: its text asks for no `+`. */
export function phoneNumberWithoutPrefix<TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<string | null, SchemaPathRules.Supported, TPathKind>,
  maxLength: number,
): void {
  validate(path, ({ value }) =>
    phoneError(value(), maxLength, { kind: 'phone_digits_without_prefix' }),
  );
}

/**
 * A start and an end time (`HH:mm`): when both are set, the end must come after the start (the same
 * time is an error). Each field gets its own error, so the message shows under the one the user
 * changed.
 */
export function timeRange<TPathKind extends PathKind = PathKind.Root>(
  start: SchemaPath<string | null, SchemaPathRules.Supported, TPathKind>,
  end: SchemaPath<string | null, SchemaPathRules.Supported, TPathKind>,
): void {
  validate(start, ({ value, valueOf }): TimeBeforeEndError | undefined =>
    isNotLater(valueOf(end), value()) ? { kind: 'time_before_end' } : undefined,
  );
  validate(end, ({ value, valueOf }): TimeAfterStartError | undefined =>
    isNotLater(value(), valueOf(start)) ? { kind: 'time_after_start' } : undefined,
  );
}

function isNotLater(time: string | null, other: string | null): boolean {
  return !!time && !!other && time <= other;
}

function phoneError<TDigitsError extends ValidationError>(
  value: string | null,
  maxLength: number,
  digitsError: TDigitsError,
): TDigitsError | PhoneMaxLengthError | undefined {
  const number = value ?? '';
  if (!/^[0-9 ]*$/.test(number)) {
    return digitsError;
  }
  return number.length > maxLength ? { kind: 'phone_max_length', max: maxLength } : undefined;
}

/** `DOMParser` builds an inert document: nothing in the HTML runs or loads. */
function visibleTextLength(html: string | null): number {
  return html
    ? (new DOMParser().parseFromString(html, 'text/html').body.textContent ?? '').length
    : 0;
}

function isWebLink(text: string): boolean {
  try {
    return ['http:', 'https:'].includes(new URL(text).protocol);
  } catch {
    return false;
  }
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
