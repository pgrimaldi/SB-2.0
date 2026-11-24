import { ServiceTranslation } from './service-translation';

export class HourlyServiceSelectedGeneralInfo {
  idHourlyService: string | null = null;
  translations: ServiceTranslation[] = [];
  /** Also the name of an icon that is not one of `StandardServiceIcon`. */
  icon: string | null = null;
  /** The uploaded image, null with an icon or without an image. */
  imageUrl: string | null = null;
  isHighlighted: boolean | null = null;
  version: string | null = null;
}
