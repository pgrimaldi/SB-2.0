import { PackageSectorPrice } from './package-sector-price';

export const MAX_REDUCED_SEASONAL_PRICE_NAME_LENGTH = 120;

export class PackageReducedSeasonalPrice {
  startsOn: string | null = null;
  endsOn: string | null = null;
  reducedSeasonalPriceName: string | null = null;
  sectorPrices: PackageSectorPrice[] = [];
}
