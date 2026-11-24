export class WarehouseItemAddRequest {
  idArticle: string | null = null;
  totalQuantity: number | null = null;
  lowStockThreshold: number | null = null;
  lowStockAlertEnabled: boolean | null = null;
}
