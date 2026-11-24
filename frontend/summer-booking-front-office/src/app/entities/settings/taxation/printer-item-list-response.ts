import { PrinterMode } from '../../enums/printer-mode';

export class PrinterItemListResponse {
  idPrinter: string | null = null;
  printerName: string | null = null;
  printerIP: string | null = null;
  printerMode: PrinterMode | null = null;
  isEnabled = false;
}
