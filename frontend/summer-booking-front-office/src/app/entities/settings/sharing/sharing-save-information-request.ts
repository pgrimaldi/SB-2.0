export const MAX_SHARING_BOOKING_DAYS = 366;
export const MAX_SHARING_CREDIT_UNITS = 1000;
export const MAX_SHARING_CREDIT_AMOUNT = 100_000;

export class SharingSaveInformationRequest {
  isEnabled = false;
  minBookingDays: number | null = null;
  seasonalOnly = false;
  allArticles = false;
  absencesAsSharing = false;
  manualCreditEnabled = false;
  manualCreditUnitsPerDay: number | null = null;
  manualCreditAmount: number | null = null;
  voucherCreditEnabled = false;
  voucherCreditAmount: number | null = null;
  cardTopUpEnabled = false;
  cardTopUpAmount: number | null = null;
}
