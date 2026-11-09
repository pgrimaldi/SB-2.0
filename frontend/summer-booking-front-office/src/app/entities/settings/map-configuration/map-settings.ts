export class MapBackground {
  iconId: string | null = null;
  name: string | null = null;
  url: string | null = null;
  isCustom = false;
}

export class MapIcon {
  iconId: string | null = null;
  name: string | null = null;
  url: string | null = null;
  isDefault = false;
}

export class MapIconPair {
  free = new MapIcon();
  occupied = new MapIcon();
}

export class MapCustomArticleIcons {
  variant: number | null = null;
  icons = new MapIconPair();
}

export class MapIcons {
  umbrella = new MapIconPair();
  customArticles: MapCustomArticleIcons[] = [];
}

export class MapSettings {
  rows: number | null = null;
  columns: number | null = null;
  tiltDegrees: number | null = null;
  backgroundColor: string | null = null;
  background: MapBackground | null = null;
  icons = new MapIcons();
  resetDeletesBookings = false;
}

export const MAX_MAP_BACKGROUND_MEGABYTES = 5;
