import {
  PathKind,
  SchemaPath,
  SchemaPathRules,
  ValidationError,
  validate,
} from '@angular/forms/signals';
import {
  MAX_PRICE_LIST_PRICE,
  MAX_PUBLIC_HOLIDAY_QUANTITY,
} from '../../../../entities/settings/catalog/package-add-item-request';
import { PackageIntermediatePrice } from '../../../../entities/settings/catalog/package-intermediate-price';
import { PackagePeriodPrice } from '../../../../entities/settings/catalog/package-period-price';
import { PackageReducedSeasonalPrice } from '../../../../entities/settings/catalog/package-reduced-seasonal-price';
import { PackageSectorPrice } from '../../../../entities/settings/catalog/package-sector-price';

export interface MaxPriceError extends ValidationError {
  readonly kind: 'catalog.price.max';
  readonly max: number;
}

export interface VatRangeError extends ValidationError {
  readonly kind: 'catalog.vat.range';
}

export interface MaxPublicHolidaysError extends ValidationError {
  readonly kind: 'catalog.public_holidays.max';
  readonly max: number;
}

/** A rate more than 0 and less than 100, as the backend (`ContentRules.Vat`). */
export function vatRange<TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<number | null, SchemaPathRules.Supported, TPathKind>,
): void {
  validate(path, ({ value }): VatRangeError | undefined => {
    const rate = value();
    return rate !== null && (rate <= 0 || rate >= 100) ? { kind: 'catalog.vat.range' } : undefined;
  });
}

export function maxPublicHolidays<TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<readonly string[], SchemaPathRules.Supported, TPathKind>,
): void {
  validate(path, ({ value }): MaxPublicHolidaysError | undefined =>
    value().length > MAX_PUBLIC_HOLIDAY_QUANTITY
      ? { kind: 'catalog.public_holidays.max', max: MAX_PUBLIC_HOLIDAY_QUANTITY }
      : undefined,
  );
}

/** A table of the price list with a price above the backend's limit: the message goes under the table. */
export function maxPrices<TRow, TPathKind extends PathKind = PathKind.Root>(
  path: SchemaPath<readonly TRow[], SchemaPathRules.Supported, TPathKind>,
  pricesOf: (row: TRow) => readonly (number | null)[],
): void {
  validate(path, ({ value }): MaxPriceError | undefined =>
    value().some((row) =>
      pricesOf(row).some((price) => price !== null && price > MAX_PRICE_LIST_PRICE),
    )
      ? { kind: 'catalog.price.max', max: MAX_PRICE_LIST_PRICE }
      : undefined,
  );
}

export function sectorPricesOf({
  seasonalHalfDayPrice,
  seasonalPrice,
}: PackageSectorPrice): (number | null)[] {
  return [seasonalHalfDayPrice, seasonalPrice];
}

export function periodPricesOf(row: PackagePeriodPrice): (number | null)[] {
  return [
    row.halfDayPrice,
    row.dailyPrice,
    row.weekendPrice,
    row.halfDayWeekendPrice,
    row.halfDaySevenDaysPrice,
    row.sevenDaysPrice,
    row.halfDayFourteenDaysPrice,
    row.fourteenDaysPrice,
    row.halfDayMonthlyPrice,
    row.monthlyPrice,
  ];
}

export function intermediatePricesOf({ price }: PackageIntermediatePrice): (number | null)[] {
  return [price];
}

export function reducedPricesOf({ sectorPrices }: PackageReducedSeasonalPrice): (number | null)[] {
  return sectorPrices.flatMap(sectorPricesOf);
}
