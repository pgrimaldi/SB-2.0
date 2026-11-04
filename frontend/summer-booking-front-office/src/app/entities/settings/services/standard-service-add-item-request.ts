import { StandardServiceIcon } from '../../enums/standard-service-icon';
import { ServiceBookingOption } from './service-booking-option';
import { ServicePeriodPrice } from './service-period-price';
import { ServiceTranslation } from './service-translation';

export class StandardServiceAddItemRequest {
  translations: ServiceTranslation[] = [];
  icon: StandardServiceIcon | null = null;
  isHighlighted = false;
  price: number | null = null;
  vatRate: number | null = null;
  isPeriodPricingEnabled = false;
  periodPrices: ServicePeriodPrice[] = [];
  idSectors: string[] = [];
  isOnlinePurchaseEnabled = false;
  bookingOptions: ServiceBookingOption[] = [];
}
