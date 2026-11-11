import { PackagePeriodPrice } from './package-period-price';
import { PackageSectorPrice } from './package-sector-price';
import { PackageTranslation } from './package-translation';

export const MAX_PACKAGE_ADDITIONAL_ITEM_QUANTITY = 15;
export const MAX_PACKAGE_PEOPLE_QUANTITY = 15;
export const MAX_PACKAGE_ARTICLE_QUANTITY = 10;

export class PackageAddItemRequest {
  translations: PackageTranslation[] = [];
  maxAdditionalItemQuantity: number | null = null;
  maxPeopleQuantity: number | null = null;
  umbrellaQuantity: number | null = null;
  sunbedQuantity: number | null = null;
  deckchairQuantity: number | null = null;
  directorChairQuantity: number | null = null;
  cabinQuantity: number | null = null;
  tentQuantity: number | null = null;
  includedServices: string | null = null;
  idSectors: string[] = [];
  isDefault = false;
  isDefaultOnline = false;
  isOnlineEnabled = false;
  isOnlineOnly = false;
  sectorPrices: PackageSectorPrice[] = [];
  periodPrices: PackagePeriodPrice[] = [];
  isCumulative = false;
  isPublicHolidaysIncluded = false;
}
