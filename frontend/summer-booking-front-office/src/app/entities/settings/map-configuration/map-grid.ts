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
export const MIN_MAP_TILT_DEGREES = -45;
export const MAX_MAP_TILT_DEGREES = 45;
export const MAP_CUSTOM_ARTICLE_QUANTITY = 8;

export const OCCUPIED_UMBRELLA_ICONS: readonly MapMixedSpotIcon[] = [
  MapMixedSpotIcon.MixedSpot3,
  MapMixedSpotIcon.MixedSpot4,
  MapMixedSpotIcon.MixedSpot9,
  MapMixedSpotIcon.MixedSpot13,
  MapMixedSpotIcon.MixedSpot16,
  MapMixedSpotIcon.MixedSpot18,
  MapMixedSpotIcon.MixedSpot19,
  MapMixedSpotIcon.MixedSpot32,
  MapMixedSpotIcon.MixedSpot36,
];

export const DEFAULT_MAP_GRID: Readonly<MapGrid> = {
  rowQuantity: null,
  maxRowItemQuantity: null,
  tiltDegrees: 0,
  umbrellaIcons: new MapElementIcons<MapUmbrellaIcon, MapMixedSpotIcon>(),
  customArticleIcons: Array.from({ length: MAP_CUSTOM_ARTICLE_QUANTITY }, (_, index) =>
    Object.assign(new MapArticleIcons(), { variant: index + 1 }),
  ),
};
