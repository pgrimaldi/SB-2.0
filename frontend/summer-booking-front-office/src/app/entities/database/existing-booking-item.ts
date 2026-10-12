import { ReservationKind } from '../enums/reservation-kind';
import { ReservationStatus } from '../enums/reservation-status';

export class ExistingBookingItem {
  idBooking: string | null = null;
  customerName: string | null = null;
  reservationKind: ReservationKind | null = null;
  dateFrom: string | null = null;
  dateTo: string | null = null;
  agreementName: string | null = null;
  reservationStatus: ReservationStatus | null = null;
}
