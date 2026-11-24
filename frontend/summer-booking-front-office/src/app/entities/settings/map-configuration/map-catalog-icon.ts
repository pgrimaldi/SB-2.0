import { MapIconCategory } from '../../enums/map-icon-category';

export class MapCatalogIcon {
  id: string | null = null;
  category: MapIconCategory | null = null;
  state: string | null = null;
  name: string | null = null;
  url: string | null = null;
  isDefault = false;
}
