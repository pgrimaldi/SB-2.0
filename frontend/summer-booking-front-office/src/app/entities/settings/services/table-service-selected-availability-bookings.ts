import { TableServiceAvailabilityMode } from '../../enums/table-service-availability-mode';
import { TableServiceBookingPayment } from '../../enums/table-service-booking-payment';
import { TableServiceDepositMode } from '../../enums/table-service-deposit-mode';
import { TableServiceAvailability } from './table-service-availability';
import { TableServiceTableGroup } from './table-service-table-group';

export class TableServiceSelectedAvailabilityBookings {
  availabilities: TableServiceAvailability[] = [];
  availabilityMode: TableServiceAvailabilityMode | null = null;
  tableGroups: TableServiceTableGroup[] = [];
  totalSeatQuantity: number | null = null;
  /** Also a value that is not one of `TableServiceMealDuration`, kept until another is chosen. */
  averageMealMinutes: number | null = null;
  maxBookingPerTimeSlotQuantity: number | null = null;
  /** Not in the form, as the maximum after it: sent back as read. */
  minSeatPerBookingQuantity: number | null = null;
  maxSeatPerBookingQuantity: number | null = null;
  bookingPayment: TableServiceBookingPayment | null = null;
  depositMode: TableServiceDepositMode | null = null;
  depositPercentage: number | null = null;
  /** In euro, instead of the percentage. Not in the form: sent back as read until this is written. */
  depositAmount: number | null = null;
  isOnlineBookingEnabled: boolean | null = null;
  version: string | null = null;
}
