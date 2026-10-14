export class SharingGeneralInformation {
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
