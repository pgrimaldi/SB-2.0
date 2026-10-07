export class WarehouseItem {
  idArticle: string | null = null;
  name: string | null = null;
  totalQuantity: number | null = null;
  /** `total` minus today's consumptions. */
  availableQuantity: number | null = null;
  thresholdQuantity: number | null = null;
  /** Only with a threshold above 0. */
  isThresholdWarningActive = false;
}
