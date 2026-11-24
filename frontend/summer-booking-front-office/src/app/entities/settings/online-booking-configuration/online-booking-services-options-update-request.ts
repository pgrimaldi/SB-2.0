import { OnlinePaymentMethod } from '../../enums/online-payment-method';
import { SupplementType } from '../../enums/supplement-type';

export const MAX_SURCHARGE_PERCENTAGE = 100;
export const MAX_SURCHARGE_AMOUNT = 10_000;
export const MAX_SERVICES_SECTION_TITLE_LENGTH = 2000;
export const MAX_ONLINE_USEFUL_INFO_LENGTH = 10_000;

export class OnlineBookingServicesOptionsUpdateRequest {
  isEnabled = false;
  surchargeType = SupplementType.None;
  surchargeValue: number | null = null;
  requiresManualConfirmation = false;
  sectionTitle: string | null = null;
  usefulInfo: string | null = null;
  paymentMethods: OnlinePaymentMethod[] = [];
}
