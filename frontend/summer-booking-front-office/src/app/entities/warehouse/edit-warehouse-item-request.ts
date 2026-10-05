import { AddWarehouseItemRequest } from './add-warehouse-item-request';

/**
 * Body of `POST /api/warehouse/edit-warehouse-item`: the new values of an article of the warehouse.
 * The same fields as the addition, but the article is the one to change (`idItem`) and cannot change.
 */
export interface EditWarehouseItemRequest extends Omit<AddWarehouseItemRequest, 'idArticle'> {
  /** The article to change: `idArticle` of its row in `warehouse/list`. */
  idItem: string;
}
