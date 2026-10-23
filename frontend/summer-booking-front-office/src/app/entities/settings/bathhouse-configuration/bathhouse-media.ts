import { BathhousePhoto } from './bathhouse-photo';

export class BathhouseMedia {
  photos: BathhousePhoto[] = [];
  virtualTourUrl: string | null = null;
  priceListPdfUrl: string | null = null;
  menuPdfUrl: string | null = null;
}
