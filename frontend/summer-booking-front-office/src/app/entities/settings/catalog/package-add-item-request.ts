import { PriceCalculationMethod } from '../../enums/price-calculation-method';
import { PriceMonthlySetting } from '../../enums/price-monthly-setting';
import { PriceWeekendSetting } from '../../enums/price-weekend-setting';
import { PackageIntermediatePrice } from './package-intermediate-price';
import { PackageItemQuantity } from './package-item-quantity';
import { PackagePeriodPrice } from './package-period-price';
import { PackageReducedSeasonalPrice } from './package-reduced-seasonal-price';
import { PackageSectorPrice } from './package-sector-price';
import { PackageTranslation } from './package-translation';

export const MAX_PACKAGE_ADDITIONAL_ITEM_QUANTITY = 15;
export const MAX_PACKAGE_PEOPLE_QUANTITY = 15;
export const MAX_PACKAGE_ARTICLE_QUANTITY = 10;
export const MAX_PACKAGE_INCLUDED_SERVICES_LENGTH = 1000;
export const MAX_PRICE_LIST_PRICE = 1_000_000;

export class PackageAddItemRequest {
  translations: PackageTranslation[] = [];
  maxAdditionalItemQuantity: number | null = null;
  maxPeopleQuantity: number | null = null;
  items: PackageItemQuantity[] = [];
  includedServices: string | null = null;
  idSectorAreas: string[] = [];
  idSectors: string[] = [];
  isDefault = false;
  isDefaultOnline = false;
  isOnlineEnabled = false;
  isOnlineOnly = false;
  reducedSeasonalPrices: PackageReducedSeasonalPrice[] = [];
  sectorPrices: PackageSectorPrice[] = [];
  periodPrices: PackagePeriodPrice[] = [];
  vatRate: number | null = null;
  calculationMethod: PriceCalculationMethod | null = null;
  isCumulative = false;
  monthlySetting: PriceMonthlySetting | null = null;
  weekendSetting: PriceWeekendSetting | null = null;
  isPublicHolidaysIncluded = false;
  publicHolidayDates: string[] = [];
  isIntermediatePricesIncluded = false;
  intermediatePrices: PackageIntermediatePrice[] = [];
}
