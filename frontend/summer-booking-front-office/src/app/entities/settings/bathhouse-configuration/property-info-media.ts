import { PropertyInfoDocument } from './property-info-document';
import { PropertyInfoPhoto } from './property-info-photo';

export class PropertyInfoMedia {
  mainPhotoUrl: string | null = null;
  photos: PropertyInfoPhoto[] = [];
  virtualTourUrl: string | null = null;
  latitude: number | null = null;
  longitude: number | null = null;
  priceListPdf: PropertyInfoDocument | null = null;
  menuPdf: PropertyInfoDocument | null = null;
}
