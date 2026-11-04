/** Rates of the five departments shown by the popup "Aggiungi nuova stampante". */
export const PRINTER_DEPARTMENTS_COUNT = 5;

export const VAT_RATES = ['0', '4', '5', '10', '22'];

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
  departmentVatRates: string[] = Array.from({ length: PRINTER_DEPARTMENTS_COUNT }, () => '0');
  firstHeader: string | null = null;
  secondHeader: string | null = null;
  footer: string | null = null;
}
