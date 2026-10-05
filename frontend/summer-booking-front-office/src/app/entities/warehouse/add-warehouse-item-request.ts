/** Body of `POST /api/warehouse/add-warehouse-item`: an article added to the warehouse of the property. */
export interface AddWarehouseItemRequest {
  /** Property of the user (`AuthUser.idProperty`). */
  idProperty: string;
  /** The chosen article: `id` of `warehouse/combobox-list`, the same `idArticle` of `warehouse/list`. */
  idArticle: string;
  /** Pieces added, a whole number above 0. */
  articleQuantity: number;
  /** Stock level below which the article is running out, a whole number; `null` without a threshold. */
  thresholdQuantity: number | null;
  /** The threshold alert is on; only with a threshold above 0. */
  isThresholdWarningActive: boolean;
}
