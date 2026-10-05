export interface AddWarehouseItemRequest {
  idProperty: string;
  /** `id` of `warehouse/combobox-list`, the same as `idArticle` of `warehouse/list`. */
  idArticle: string;
  /** Whole number above 0. */
  articleQuantity: number;
  thresholdQuantity: number | null;
  /** Only with a threshold above 0. */
  isThresholdWarningActive: boolean;
}
