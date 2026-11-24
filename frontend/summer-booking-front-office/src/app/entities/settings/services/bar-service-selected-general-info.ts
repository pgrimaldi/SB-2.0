import { BarServiceCategory } from '../../enums/bar-service-category';
import { ServiceTranslation } from './service-translation';

export class BarServiceSelectedGeneralInfo {
  idBarService: string | null = null;
  translations: ServiceTranslation[] = [];
  category: BarServiceCategory | null = null;
  /** Also the name of an icon that is not one of `BarServiceIcon`. */
  icon: string | null = null;
  /** The uploaded image, null with an icon or without an image. */
  imageUrl: string | null = null;
  isHighlighted: boolean | null = null;
  version: string | null = null;
}
