import { PackageSectorPrice } from './package-sector-price';

export class PackageReducedSeasonalPrice {
  dateFrom: string | null = null;
  dateTo: string | null = null;
  reducedSeasonalPriceName: string | null = null;
  sectorPrices: PackageSectorPrice[] = [];
}
