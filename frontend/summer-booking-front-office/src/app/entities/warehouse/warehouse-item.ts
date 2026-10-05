export interface WarehouseItem {
  idArticle: string;
  name: string;
  totalQuantity: number;
  /** `total` minus today's consumptions. */
  availableQuantity: number;
  thresholdQuantity: number | null;
  /** Only with a threshold above 0. */
  isThresholdWarningActive: boolean;
}
