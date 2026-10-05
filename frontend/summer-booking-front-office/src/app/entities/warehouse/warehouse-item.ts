export interface WarehouseItem {
  idArticle: string;
  name: string;
  total: number;
  /** `total` minus today's consumptions. */
  available: number;
  thresholdNumber: number | null;
  /** Only with a threshold above 0. */
  isThresholdWarningActive: boolean;
}
