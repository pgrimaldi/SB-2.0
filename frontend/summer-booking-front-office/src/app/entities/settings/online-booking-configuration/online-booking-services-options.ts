import { SupplementType } from '../../enums/supplement-type';

export class OnlineBookingServicesOptions {
  isServicesOnlineBookingEnabled = false;
  supplementType = SupplementType.None;
  supplementValue: number | null = null;
  isBookingConfirmationRequired = false;
  isPayNowAccepted = false;
  isPayAtPropertyAccepted = false;
  isPayAtPropertyWithoutCardAccepted = false;
  isDepositAccepted = false;
  isPrepaidCardAccepted = false;
  bookingPanelTitle: string | null = null;
  usefulInformation: string | null = null;
}
