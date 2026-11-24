export const MAX_TABLE_QUANTITY = 500;
export const MAX_SEAT_PER_TABLE_QUANTITY = 100;
export const MAX_TOTAL_SEAT_QUANTITY = 10_000;

export class TableServiceTableGroup {
  tableQuantity: number | null = null;
  seatPerTableQuantity: number | null = null;
}
