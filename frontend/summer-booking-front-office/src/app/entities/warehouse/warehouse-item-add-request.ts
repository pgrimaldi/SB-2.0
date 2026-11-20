export class WarehouseItemAddRequest {
  idArticle: string | null = null;
  totalQuantity: number | null = null;
  thresholdQuantity: number | null = null;
  isThresholdWarningActive: boolean | null = null;
}
