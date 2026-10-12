export class WarehouseItem {
  idArticle: string | null = null;
  name: string | null = null;
  totalQuantity: number | null = null;
  availableQuantity: number | null = null;
  thresholdQuantity: number | null = null;
  isThresholdWarningActive = false;
}
