import { PriceCalculationMethod } from '../../enums/price-calculation-method';
import { PriceMonthlySetting } from '../../enums/price-monthly-setting';
import { PriceWeekendSetting } from '../../enums/price-weekend-setting';
import { PackagePeriodPrice } from './package-period-price';
import { PackageReducedSeasonalPrice } from './package-reduced-seasonal-price';
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
  idSectorAreas: string[] = [];
  idSectors: string[] = [];
  isDefault = false;
  isDefaultOnline = false;
  isOnlineEnabled = false;
  isOnlineOnly = false;
  reducedSeasonalPrice: PackageReducedSeasonalPrice | null = null;
  sectorPrices: PackageSectorPrice[] = [];
  periodPrices: PackagePeriodPrice[] = [];
  calculationMethod: PriceCalculationMethod | null = null;
  isCumulative = false;
  monthlySetting: PriceMonthlySetting | null = null;
  weekendSetting: PriceWeekendSetting | null = null;
  isPublicHolidaysIncluded = false;
}
