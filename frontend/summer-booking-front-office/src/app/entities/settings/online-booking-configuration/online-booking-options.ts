import { OnlineBookingPeriod } from './online-booking-period';

export class OnlineBookingOptions {
  isOnlineBookingEnabled = false;
  isMapSpotChoiceEnabled = false;
  isSelectedPeriodsOnlyEnabled = false;
  selectedPeriods: OnlineBookingPeriod[] = [];
  isSelectedSpotsOnlyEnabled = false;
}
