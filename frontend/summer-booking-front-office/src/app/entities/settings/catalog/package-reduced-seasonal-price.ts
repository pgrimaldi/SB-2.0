import { PackageSectorPrice } from './package-sector-price';

export class PackageReducedSeasonalPrice {
  startsOn: string | null = null;
  endsOn: string | null = null;
  reducedSeasonalPriceName: string | null = null;
  sectorPrices: PackageSectorPrice[] = [];
}
