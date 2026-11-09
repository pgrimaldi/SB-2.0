import { MapMixedSpotIcon } from '../../enums/map-mixed-spot-icon';
import { MapUmbrellaIcon } from '../../enums/map-umbrella-icon';

export class MapElementIcons<FreeIcon, OccupiedIcon = FreeIcon> {
  freeIcon: FreeIcon | null = null;
  occupiedIcon: OccupiedIcon | null = null;
}

export class MapArticleIcons extends MapElementIcons<MapMixedSpotIcon> {
  variant: number | null = null;
}

export class MapGrid {
  rowQuantity: number | null = null;
  maxRowItemQuantity: number | null = null;
  tiltDegrees: number | null = null;
  umbrellaIcons = new MapElementIcons<MapUmbrellaIcon, MapMixedSpotIcon>();
  customArticleIcons: MapArticleIcons[] = [];
}

export const MAX_MAP_ROW_QUANTITY = 50;
export const MAX_MAP_ROW_ITEM_QUANTITY = 80;
export const MAX_MAP_TILT_DEGREES = 359;
export const MAP_CUSTOM_ARTICLE_QUANTITY = 8;

export const DEFAULT_MAP_GRID: Readonly<MapGrid> = {
  rowQuantity: 1,
  maxRowItemQuantity: 1,
  tiltDegrees: 0,
  umbrellaIcons: new MapElementIcons<MapUmbrellaIcon, MapMixedSpotIcon>(),
  customArticleIcons: Array.from({ length: MAP_CUSTOM_ARTICLE_QUANTITY }, (_, index) =>
    Object.assign(new MapArticleIcons(), { variant: index + 1 }),
  ),
};
