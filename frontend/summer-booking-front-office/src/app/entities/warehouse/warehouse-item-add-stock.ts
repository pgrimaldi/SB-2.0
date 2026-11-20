export class WarehouseItemAddStock {
  idArticle: string | null = null;
  totalQuantity: number | null = null;
  lowStockThreshold: number | null = null;
  lowStockAlertEnabled = false;
}
