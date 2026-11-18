import { BathhousePhoto } from './bathhouse-photo';

export const MAX_BATHHOUSE_PHOTO_QUANTITY = 30;
export const MAX_BATHHOUSE_PDF_MEGABYTES = 10;
export const MAX_VIRTUAL_TOUR_URL_LENGTH = 500;

export class BathhouseMedia {
  mainPhotoUrl: string | null = null;
  photos: BathhousePhoto[] = [];
  virtualTourUrl: string | null = null;
  latitude: number | null = null;
  longitude: number | null = null;
  priceListPdfUrl: string | null = null;
  priceListPdfFileName: string | null = null;
  menuPdfUrl: string | null = null;
  menuPdfFileName: string | null = null;
}
