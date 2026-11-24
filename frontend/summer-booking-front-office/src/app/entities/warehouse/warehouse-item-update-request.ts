export class WarehouseItemUpdateRequest {
  articleCode: string | null = null;
  name: string | null = null;
  totalQuantity: number | null = null;
  lowStockThreshold: number | null = null;
  lowStockAlertEnabled = false;
}
