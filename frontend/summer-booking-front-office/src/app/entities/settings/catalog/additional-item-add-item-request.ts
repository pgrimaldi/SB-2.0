import { PriceCalculationMethod } from '../../enums/price-calculation-method';
import { PriceMonthlySetting } from '../../enums/price-monthly-setting';
import { PriceWeekendSetting } from '../../enums/price-weekend-setting';
import { PackageIntermediatePrice } from './package-intermediate-price';
import { PackagePeriodPrice } from './package-period-price';
import { PackageSectorPrice } from './package-sector-price';

export class AdditionalItemAddItemRequest {
  idArticle: string | null = null;
  idPackages: string[] = [];
  isOnlineEnabled = false;
  isOnlineOnly = false;
  isMapAssignable = false;
  sectorPrices: PackageSectorPrice[] = [];
  periodPrices: PackagePeriodPrice[] = [];
  calculationMethod: PriceCalculationMethod | null = null;
  isCumulative = false;
  monthlySetting: PriceMonthlySetting | null = null;
  weekendSetting: PriceWeekendSetting | null = null;
  isPublicHolidaysIncluded = false;
  publicHolidayDates: string[] = [];
  isIntermediatePricesIncluded = false;
  intermediatePrices: PackageIntermediatePrice[] = [];
}
