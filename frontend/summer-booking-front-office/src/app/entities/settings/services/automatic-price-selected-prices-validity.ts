import { AutomaticPriceValidityPeriod } from './automatic-price-validity-period';

export class AutomaticPriceSelectedPricesValidity {
  price: number | null = null;
  vatRate: number | null = null;
  isOnlinePurchaseEnabled: boolean | null = null;
  validityPeriods: AutomaticPriceValidityPeriod[] = [];
  isAllServicesEnabled: boolean | null = null;
  idBarServices: string[] = [];
  idStandardServices: string[] = [];
  idHourlyServices: string[] = [];
  isPricePerServiceEnabled: boolean | null = null;
  idSectors: string[] = [];
  version: string | null = null;
}
