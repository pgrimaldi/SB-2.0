import { AutomaticPriceValidityPeriod } from './automatic-price-validity-period';

export class AutomaticPriceUpdatePricesValidityRequest {
  idAutomaticPrice: string | null = null;
  price: number | null = null;
  vatRate: number | null = null;
  isOnlinePurchaseEnabled = false;
  validityPeriods: AutomaticPriceValidityPeriod[] = [];
  isAllServicesEnabled = false;
  /** Without `isAllServicesEnabled`, as the two lists after it. */
  idBarServices: string[] = [];
  idStandardServices: string[] = [];
  idHourlyServices: string[] = [];
  isPricePerServiceEnabled = false;
  idSectors: string[] = [];
}
