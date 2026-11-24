import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { SectorItem } from '../../../entities/settings/rows-and-sectors/sector-item';

export interface SectorUsage {
  mapSpots: number;
  priceLists: number;
  stayLimits: number;
  discounts: number;
  services: number;
  automaticPrices: number;
}

export type SectorRow = Omit<SectorItem, 'version'> & {
  propertyId: number;
  sortOrder: number;
  usage?: Partial<SectorUsage>;
};

const SECTOR_NAMES = [
  'Fila 1',
  'Fila 2',
  'Fila 3',
  'Fila 4',
  'Fila 5',
  'Fila 6',
  'Fila 7',
  'Fila 8',
  'Settore Est',
  'Settore Ovest',
  'Gazebo',
  'Zona relax',
];

export const DELETED_SECTOR = '5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4aff';

export const SECTORS: SectorRow[] = SECTOR_NAMES.map((sectorName, index) => ({
  propertyId: 1,
  idSector: `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a${String(index + 1).padStart(2, '0')}`,
  sectorName,
  sortOrder: index,
  ...(index === 0 ? { usage: { mapSpots: 14, priceLists: 1 } } : {}),
}));

export const SECTOR_AREAS: (ComboboxItem & { propertyId: number })[] = [
  { propertyId: 1, id: '9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01', value: 'Area lettini' },
  { propertyId: 1, id: '9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a02', value: 'Area ombrelloni' },
  { propertyId: 1, id: '9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a03', value: 'Area cabine' },
  { propertyId: 1, id: '9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a04', value: 'Parcheggio' },
];
