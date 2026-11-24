import { OnlineBookingAudience } from '../../enums/online-booking-audience';
import { OnlineBookingBlock } from '../../enums/online-booking-block';
import { OnlinePaymentMethod } from '../../enums/online-payment-method';
import { SupplementType } from '../../enums/supplement-type';
import { BookingLimitRule } from './booking-limit-rule';
import { LegendItem } from './legend-item';
import { OnlineBookingPeriod } from './online-booking-period';

export const MAX_ONLINE_BOOKING_PERIOD_QUANTITY = 50;
export const MAX_ONLINE_BOOKING_HORIZON_DAYS = 730;
/** The limit of the column that keeps spots and bookings. */
export const MAX_ONLINE_BOOKING_QUANTITY = 32_767;

export class OnlineBookingOptionsUpdateRequest {
  isEnabled = false;
  isMapPick = false;
  isLocationRestricted = false;
  allowedSpotIds: string[] = [];
  isPeriodRestricted = false;
  allowedPeriods: OnlineBookingPeriod[] = [];
  allowsHalfDay = false;
  morningFrom: string | null = null;
  morningTo: string | null = null;
  afternoonFrom: string | null = null;
  afternoonTo: string | null = null;
  afternoonConversionTime: string | null = null;
  allowsSameDayBooking = false;
  allowsSeasonal = false;
  allowsSharing = false;
  surchargeType = SupplementType.None;
  surchargeValue: number | null = null;
  onlineBookingBlock = OnlineBookingBlock.Disabled;
  isBlocked = false;
  maxSpotsPerBooking: number | null = null;
  bookingHorizonDays: number | null = null;
  requiresManualConfirmation = false;
  scope = OnlineBookingAudience.All;
  maxBookingsPerDay: number | null = null;
  stayLimits: BookingLimitRule[] = [];
  paymentMethods: OnlinePaymentMethod[] = [];
  legend: LegendItem[] = [];
  goodToKnow: string | null = null;
  usefulInfo: string | null = null;
}
