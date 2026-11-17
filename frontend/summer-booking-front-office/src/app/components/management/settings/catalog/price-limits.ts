import {
  PathKind,
  SchemaPath,
  SchemaPathRules,
  ValidationError,
  validate,
} from '@angular/forms/signals';
import { MAX_PRICE_LIST_PRICE } from '../../../../entities/settings/catalog/package-add-item-request';
import { PackageIntermediatePrice } from '../../../../entities/settings/catalog/package-intermediate-price';
import { PackagePeriodPrice } from '../../../../entities/settings/catalog/package-period-price';
import { PackageReducedSeasonalPrice } from '../../../../entities/settings/catalog/package-reduced-seasonal-price';
import { PackageSectorPrice } from '../../../../entities/settings/catalog/package-sector-price';

export interface MaxPriceError extends ValidationError {
  readonly kind: 'catalog.price.max';
  readonly max: number;
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
