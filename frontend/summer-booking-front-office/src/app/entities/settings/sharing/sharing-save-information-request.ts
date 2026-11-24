export const MAX_SHARING_QUANTITY = 2_147_483_647;
export const MAX_SHARING_CREDIT_VALUE = 100_000;

export class SharingSaveInformationRequest {
  isSharingEnabled = false;
  isMinimumBookingDayEnabled = false;
  minimumBookingDayQuantity: number | null = null;
  isOnlySeasonalEnabled = false;
  isEnabledOnAllArticles = false;
  isScheduledAbsenceSharing = false;
  isManualCreditAssignmentEnabled = false;
  resoldDayCreditQuantity: number | null = null;
  manualCreditValue: number | null = null;
  isAutomaticVoucherEnabled = false;
  voucherCreditValue: number | null = null;
  isPrepaidCardTopUpEnabled = false;
  prepaidCardCreditValue: number | null = null;
}
