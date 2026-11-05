import { TableServiceAvailabilityMode } from '../../enums/table-service-availability-mode';
import { TableServiceBookingPayment } from '../../enums/table-service-booking-payment';
import { TableServiceDepositMode } from '../../enums/table-service-deposit-mode';
import { TableServiceMealDuration } from '../../enums/table-service-meal-duration';
import { TableServiceAvailability } from './table-service-availability';
import { TableServiceTableGroup } from './table-service-table-group';

export class TableServiceSelectedAvailabilityBookings {
  availabilities: TableServiceAvailability[] = [];
  availabilityMode: TableServiceAvailabilityMode | null = null;
  tableGroups: TableServiceTableGroup[] = [];
  totalSeatQuantity: number | null = null;
  averageMealDuration: TableServiceMealDuration | null = null;
  maxBookingPerTimeSlotQuantity: number | null = null;
  bookingPayment: TableServiceBookingPayment | null = null;
  depositMode: TableServiceDepositMode | null = null;
  depositPercentage: number | null = null;
  isOnlineBookingEnabled: boolean | null = null;
}
