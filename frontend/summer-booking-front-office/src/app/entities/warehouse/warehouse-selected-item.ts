export class WarehouseSelectedItem {
  id: string | null = null;
  articleCode: string | null = null;
  name: string | null = null;
  totalQuantity: number | null = null;
  lowStockThreshold: number | null = null;
  lowStockAlertEnabled = false;
  mapPlaceable = false;
  version: string | null = null;
}
