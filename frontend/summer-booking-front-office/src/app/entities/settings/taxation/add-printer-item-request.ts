/** Rates of the five departments shown by the popup "Aggiungi nuova stampante". */
export const PRINTER_DEPARTMENTS_COUNT = 5;

/**
 * The form of the popup "Aggiungi nuova stampante". Field names are provisional: the API that saves
 * a printer is not defined yet.
 */
export class AddPrinterItemRequest {
  printerName: string | null = null;
  printerIP: string | null = null;
  printerPort: number | null = null;
  printerModel: string | null = null;
  isSsl = false;
  isFiscalMode = false;
  isNonFiscalMode = false;
  isOrderMode = false;
  isTextMode = false;
  /** One VAT rate per department, in percent ('0' = 0%). */
  departmentVatRates: string[] = Array.from({ length: PRINTER_DEPARTMENTS_COUNT }, () => '0');
  firstHeader: string | null = null;
  secondHeader: string | null = null;
  footer: string | null = null;
}
