import { AddWarehouseItemRequest } from './add-warehouse-item-request';

export interface EditWarehouseItemRequest extends Omit<AddWarehouseItemRequest, 'idArticle'> {
  /** `idArticle` of the row in `warehouse/list`; the article itself cannot change. */
  idItem: string;
}
