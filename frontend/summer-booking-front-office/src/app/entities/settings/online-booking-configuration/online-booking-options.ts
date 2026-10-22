import { SupplementType } from '../../enums/supplement-type';
import { OnlineBookingPeriod } from './online-booking-period';

export class OnlineBookingOptions {
  isOnlineBookingEnabled = false;
  isMapSpotChoiceEnabled = false;
  isSelectedPeriodsOnlyEnabled = false;
  selectedPeriods: OnlineBookingPeriod[] = [];
  isSelectedSpotsOnlyEnabled = false;
  isHalfDayBookingEnabled = false;
  morningStartTime: string | null = null;
  morningEndTime: string | null = null;
  afternoonStartTime: string | null = null;
  afternoonEndTime: string | null = null;
  isAfternoonConversionTimeEnabled = false;
  isCurrentDayBookingEnabled = false;
  isSeasonalBookingEnabled = false;
  isSharingEnabled = false;
  supplementType = SupplementType.None;
  supplementValue: number | null = null;
}
