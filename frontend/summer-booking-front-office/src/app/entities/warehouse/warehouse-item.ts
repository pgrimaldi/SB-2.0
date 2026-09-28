/** Warehouse article of the property, as answered by `GET /api/warehouse/list`. */
export interface WarehouseItem {
  idArticle: string;
  name: string;
  /** Quantity owned by the property. */
  total: number;
  /** Quantity still free today: total minus today's consumptions. */
  available: number;
  /** Stock level below which the article is running out. */
  thresholdNumber: number | null;
}
