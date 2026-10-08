import { DateStyle, NumberStyle } from './message-parser';

/** A day without time (`2026-08-01`): shown as it is, never moved by the time zone. */
const DAY = /^\d{4}-\d{2}-\d{2}$/;
/** ISO 4217 currency code, e.g. `EUR`. */
const CURRENCY = /^[A-Z]{3}$/;

/** `Intl` formatters are costly to create: one per locale and options, reused. */
const cache = new Map<string, Intl.NumberFormat | Intl.DateTimeFormat | Intl.PluralRules>();

function cached<T extends Intl.NumberFormat | Intl.DateTimeFormat | Intl.PluralRules>(
  key: string,
  create: () => T,
): T {
  let formatter = cache.get(key) as T | undefined;
  if (!formatter) {
    formatter = create();
    cache.set(key, formatter);
  }
  return formatter;
}

/** Accepts numeric strings too; `null` when not a finite number. */
export function toNumber(value: unknown): number | null {
  const number = typeof value === 'string' && value.trim() ? Number(value) : value;
  return typeof number === 'number' && Number.isFinite(number) ? number : null;
}

/**
 * With the `currency` style the code comes from `currency` (ISO 4217), or from `defaultCurrency`
 * when it is missing or not valid.
 */
export function formatNumber(
  value: number,
  style: NumberStyle,
  locale: string | undefined,
  currency: unknown,
  defaultCurrency: string,
): string {
  const options: Intl.NumberFormatOptions =
    style === NumberStyle.Integer
      ? { maximumFractionDigits: 0 }
      : style === NumberStyle.Percent
        ? { style: 'percent' }
        : style === NumberStyle.Currency
          ? {
              style: 'currency',
              currency:
                typeof currency === 'string' && CURRENCY.test(currency)
                  ? currency
                  : defaultCurrency,
            }
          : {};
  const key = `number|${locale}|${JSON.stringify(options)}`;
  return cached(key, () => new Intl.NumberFormat(locale, options)).format(value);
}

/**
 * A date or time in the locale, from ISO 8601: a moment (`2026-08-01T10:00:00Z`) in the user's time
 * zone, a day (`2026-08-01`) as it is. `null` when the value is not a date.
 */
export function formatDate(
  value: unknown,
  isHourOnly: boolean,
  style: DateStyle,
  locale: string | undefined,
): string | null {
  if (typeof value !== 'string' && typeof value !== 'number') {
    return null;
  }
  const day = typeof value === 'string' && DAY.test(value);
  const date = new Date(day ? `${value}T00:00:00Z` : value);
  if (Number.isNaN(date.getTime())) {
    return null;
  }
  const options: Intl.DateTimeFormatOptions = {
    [isHourOnly ? 'timeStyle' : 'dateStyle']: style,
    // A day has no time: read in UTC it stays the same day everywhere.
    ...(day && { timeZone: 'UTC' }),
  };
  const key = `date|${locale}|${JSON.stringify(options)}`;
  return cached(key, () => new Intl.DateTimeFormat(locale, options)).format(date);
}

export function pluralCategory(value: number, locale: string | undefined): string {
  return cached(`plural|${locale}`, () => new Intl.PluralRules(locale)).select(value);
}

/** The locale itself when `Intl` accepts it, otherwise the browser's (`undefined`). */
export function supportedLocale(locale: string): string | undefined {
  try {
    return Intl.getCanonicalLocales(locale)[0];
  } catch {
    return undefined;
  }
}
