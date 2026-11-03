import { BarServiceCategory } from '../../enums/bar-service-category';
import { BarServiceIcon } from '../../enums/bar-service-icon';
import { ServiceBookingOption } from './service-booking-option';
import { ServicePeriodPrice } from './service-period-price';
import { ServiceTranslation } from './service-translation';
import { ServiceVariant } from './service-variant';

export class BarServiceAddItemRequest {
  translations: ServiceTranslation[] = [];
  category: BarServiceCategory | null = null;
  icon: BarServiceIcon | null = null;
  isHighlighted = false;
  price: number | null = null;
  vatRate: number | null = null;
  variants: ServiceVariant[] = [];
  isPeriodPricingEnabled = false;
  periodPrices: ServicePeriodPrice[] = [];
  idSectors: string[] = [];
  isOnlinePurchaseEnabled = false;
  bookingOptions: ServiceBookingOption[] = [];
}
