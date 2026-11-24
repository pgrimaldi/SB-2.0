/** The fields of a row that hold a price. */
export type PriceField<T> = Exclude<
  { [K in keyof T]: T[K] extends number | null ? K : never }[keyof T],
  'idPeriod'
>;
