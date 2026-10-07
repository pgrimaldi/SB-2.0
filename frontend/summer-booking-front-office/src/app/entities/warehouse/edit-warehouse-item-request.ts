import { AddWarehouseItemRequest } from './add-warehouse-item-request';

export interface EditWarehouseItemRequest extends Omit<AddWarehouseItemRequest, 'idArticle'> {
  /** `idArticle` of the row in `warehouse/list`; the article itself cannot change. */
  idItem: string;
}

/** The form of the edit popup: an emptied field is null, sent only once the form is valid. */
export interface EditedWarehouseQuantities {
  articleQuantity: number | null;
  thresholdQuantity: number | null;
}
