import { AutomaticPriceValidityPeriod } from './automatic-price-validity-period';

export class AutomaticPriceUpdatePricesValidityRequest {
  idAutomaticPrice: string | null = null;
  price: number | null = null;
  vatRate: number | null = null;
  isOnlinePurchaseEnabled = false;
  validityPeriods: AutomaticPriceValidityPeriod[] = [];
  idBarServices: string[] = [];
  idStandardServices: string[] = [];
  idHourlyServices: string[] = [];
  isPricePerServiceEnabled = false;
  idSectors: string[] = [];
}
