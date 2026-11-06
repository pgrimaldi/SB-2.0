export class MapGrid {
  rowQuantity: number | null = null;
  maxRowItemQuantity: number | null = null;
  tiltDegrees: number | null = null;
}

export const MAX_MAP_ROW_QUANTITY = 50;
export const MAX_MAP_ROW_ITEM_QUANTITY = 80;
export const MAX_MAP_TILT_DEGREES = 359;

export const DEFAULT_MAP_GRID: Readonly<MapGrid> = {
  rowQuantity: 1,
  maxRowItemQuantity: 1,
  tiltDegrees: 0,
};
