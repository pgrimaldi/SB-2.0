import { PackagePeriodPrice } from './package-period-price';
import { PackageSectorPrice } from './package-sector-price';
import { PackageTranslation } from './package-translation';

export class PackageAddItemRequest {
  translations: PackageTranslation[] = [];
  isDefault = false;
  isDefaultOnline = false;
  isOnlineEnabled = false;
  isOnlineOnly = false;
  sectorPrices: PackageSectorPrice[] = [];
  periodPrices: PackagePeriodPrice[] = [];
  isCumulative = false;
  isPublicHolidaysIncluded = false;
}
