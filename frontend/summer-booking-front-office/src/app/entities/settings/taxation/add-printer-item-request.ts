import { PrinterMode } from '../../enums/printer-mode';
import { PrinterModel } from '../../enums/printer-model';

export const PRINTER_DEPARTMENTS_COUNT = 5;
export const VAT_RATES = [0, 4, 5, 10, 22];
export const MAX_PRINTER_NAME_LENGTH = 120;
export const MAX_PRINTER_IP_LENGTH = 255;
export const MAX_PRINTER_PORT = 65_535;
export const MAX_PRINTER_LINE_LENGTH = 160;

export class AddPrinterItemRequest {
  printerName: string | null = null;
  printerIP: string | null = null;
  printerPort: number | null = null;
  printerModel: PrinterModel | null = null;
  printerMode: PrinterMode | null = null;
  isSsl = false;
  departmentVatRates: (number | null)[] = Array.from(
    { length: PRINTER_DEPARTMENTS_COUNT },
    () => null,
  );
  firstHeader: string | null = null;
  secondHeader: string | null = null;
  footer: string | null = null;
}
