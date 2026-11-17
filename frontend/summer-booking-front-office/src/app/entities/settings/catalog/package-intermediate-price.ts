export const MAX_INTERMEDIATE_PRICE_DAY_QUANTITY = 90;

export class PackageIntermediatePrice {
  idPeriod: number | null = null;
  isFirstRow = false;
  dayQuantity: number | null = null;
  price: number | null = null;
}
