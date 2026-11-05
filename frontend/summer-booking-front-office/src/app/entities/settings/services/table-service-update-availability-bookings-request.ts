import { TableServiceAvailabilityMode } from '../../enums/table-service-availability-mode';
import { TableServiceBookingPayment } from '../../enums/table-service-booking-payment';
import { TableServiceDepositMode } from '../../enums/table-service-deposit-mode';
import { TableServiceMealDuration } from '../../enums/table-service-meal-duration';
import { TableServiceAvailability } from './table-service-availability';
import { TableServiceTableGroup } from './table-service-table-group';

export class TableServiceUpdateAvailabilityBookingsRequest {
  idTableService: string | null = null;
  availabilities: TableServiceAvailability[] = [];
  availabilityMode = TableServiceAvailabilityMode.Tables;
  tableGroups: TableServiceTableGroup[] = [];
  totalSeatQuantity: number | null = null;
  averageMealDuration: TableServiceMealDuration | null = null;
  maxBookingPerTimeSlotQuantity: number | null = null;
  bookingPayment = TableServiceBookingPayment.None;
  depositMode = TableServiceDepositMode.Total;
  depositPercentage: number | null = null;
  isOnlineBookingEnabled = false;
}
