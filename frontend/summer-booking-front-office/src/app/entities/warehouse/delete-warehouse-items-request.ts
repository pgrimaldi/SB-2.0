/** One id for the button of a row, several for the delete of the chosen rows. */
export interface DeleteWarehouseItemsRequest {
  idProperty: string;
  idItems: string[];
}
